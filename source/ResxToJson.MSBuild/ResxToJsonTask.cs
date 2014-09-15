// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResxToJsonTask.cs">
//   Copyright belongs to Manish Kumar
// </copyright>
// <summary>
//   Build task to convert Resource file to Java script Object Notation file
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ResxToJson.MSBuild
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Resources;
    using System.Web.Script.Serialization;

    using Microsoft.Build.Framework;

    /// <summary>
    /// Build task to convert Resource file to Java script Object Notation file
    /// </summary>
    public class ResxToJsonTask : ITask
    {
        /// <summary>
        /// Gets or sets Build Engine
        /// </summary>
        public IBuildEngine BuildEngine { get; set; }

        /// <summary>
        /// Gets or sets Host Object
        /// </summary>
        public ITaskHost HostObject { get; set; }

        /// <summary>
        /// Gets or sets list of EmbeddedResource Files
        /// </summary>
        [Required]
        public ITaskItem[] EmbeddedResourcesItems { get; set; }

        /// <summary>
        /// Gets or sets Project Full Path
        /// </summary>
        [Required]
        public string ProjectFullPath { get; set; }

        /// <summary>
        /// Gets or sets Project Output Path
        /// </summary>
        [Required]
        public string OutputPath { get; set; }

        /// <summary>
        /// Executes the Task
        /// </summary>
        /// <returns>True if success</returns>
        public bool Execute()
        {
            if (!this.EmbeddedResourcesItems.Any())
            {
                this.BuildEngine.LogMessageEvent(
                    new BuildMessageEventArgs(
                        string.Format(
                            "Skipping conversion of Resource files to json, as there are no resource files found in the project. If your resx file is not being picked up, check if the file is marked for build action = 'Embedded Resource'"),
                        string.Empty,
                        "ResxToJson",
                        MessageImportance.Normal));
            }

            var args = new BuildMessageEventArgs(
                "Started converting Resx To JSON",
                string.Empty,
                "ResxToJson",
                MessageImportance.Normal);

            var outputFullPath = Path.Combine(this.ProjectFullPath, this.OutputPath);

            this.BuildEngine.LogMessageEvent(args);
            foreach (var embeddedResourcesItem in this.EmbeddedResourcesItems)
            {
                this.BuildEngine.LogMessageEvent(
                    new BuildMessageEventArgs(
                        string.Format("Started converting Resx {0}", embeddedResourcesItem.ItemSpec),
                        string.Empty,
                        "ResxToJson",
                        MessageImportance.Normal));

                var outputFileName = Path.Combine(
                    outputFullPath,
                    Path.GetFileNameWithoutExtension(embeddedResourcesItem.ItemSpec) + ".json");

                var content = this.GetJsonContent(embeddedResourcesItem.ItemSpec);

                using (var file = new StreamWriter(outputFileName))
                {
                    file.Write(content);
                }

                this.BuildEngine.LogMessageEvent(
                    new BuildMessageEventArgs(
                        string.Format("Generated file {0}", outputFileName),
                        string.Empty,
                        "ResxToJson",
                        MessageImportance.Normal));
            }

            return true;
        }

        /// <summary>
        /// The get JSON content.
        /// </summary>
        /// <param name="resourceItem">
        /// The resource item.
        /// </param>     
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string GetJsonContent(string resourceItem)
        {
            var cultureInfo = this.GetCultureInfo(resourceItem);
            var json = this.GetJson(resourceItem, cultureInfo);

            var jsonNamespace = Path.GetFileNameWithoutExtension(resourceItem);
            if (cultureInfo != null)
            {
                // this will get rid of locale from the name
                jsonNamespace = Path.GetFileNameWithoutExtension(jsonNamespace);
            }

            return string.Format("var {0}.Strings = {1};", jsonNamespace, json);
        }

        /// <summary>
        /// The get JSON from the resource.
        /// </summary>
        /// <param name="resourceItem">
        /// The resource item.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture Info.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string GetJson(string resourceItem, CultureInfo cultureInfo)
        {
            var strings = new Dictionary<string, object>();
            using (var rsxr = new ResXResourceReader(resourceItem))
            {
                rsxr.UseResXDataNodes = true;
                strings = rsxr.Cast<DictionaryEntry>()
                    .ToDictionary(
                        x => x.Key.ToString(),
                        x => ((ResXDataNode)x.Value).GetValue((ITypeResolutionService)null));
            }

            strings.Add("lcid", cultureInfo == null ? 0 : cultureInfo.LCID);
            strings.Add("lang", cultureInfo == null ? string.Empty : cultureInfo.Name);

            return new JavaScriptSerializer().Serialize(strings);
        }

        /// <summary>
        /// The get culture info.
        /// </summary>
        /// <param name="resourceItem">
        /// The resource item.
        /// </param>
        /// <returns>
        /// The <see cref="CultureInfo"/>.
        /// </returns>
        private CultureInfo GetCultureInfo(string resourceItem)
        {           
            var fileName = Path.GetFileNameWithoutExtension(resourceItem);

            // assuming the file name is of the format xyz.en-us.resx, xyx.abc.en-us.resx or xyx.resx
            var lang = Path.GetExtension(fileName);
            if (!string.IsNullOrEmpty(lang))
            {
                try
                {
                    return new CultureInfo(lang.Trim('.'));
                }
                catch (Exception)
                {
                }
            }
            return null;
        }
    }
}