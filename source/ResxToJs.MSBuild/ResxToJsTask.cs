// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResxToJsTask.cs">
//   Copyright belongs to Manish Kumar
// </copyright>
// <summary>
//   Build task to convert Resource file to Java script Object Notation file
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ResxToJs.MSBuild
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Resources;
    using System.Text;
    using System.Web.Script.Serialization;

    using Microsoft.Build.Framework;

    /// <summary>
    /// Build task to convert Resource file to Java script file
    /// </summary>
    public class ResxToJsTask : ITask
    {
	    /// <summary>
	    /// The JSON format.
	    /// </summary>
	    public const string OutputJsonFormat = @"{0}{1} = (function () {{ 
	var strings = {2};
	return $.extend({{}}, {1} || {{}}, strings);
}}());";

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
        public string ProjectPath { get; set; }

        /// <summary>
        /// Gets or sets Project Output Path
        /// </summary>
        [Required]
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets Assembly Name
        /// </summary>
        [Required]
        public ITaskItem AssemblyName { get; set; }

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
                            "Skipping conversion of Resource files to js, as there are no resource files found in the project. If your resx file is not being picked up, check if the file is marked for build action = 'Embedded Resource'"),
                        string.Empty,
                        "ResxToJson",
                        MessageImportance.Normal));
                return false;
            }

            var args = new BuildMessageEventArgs(
                "Started converting Resx To JS",
                string.Empty,
                "ResxToJs",
                MessageImportance.Normal);

            var outputFullPath = Path.Combine(this.ProjectPath, this.OutputPath);

            this.BuildEngine.LogMessageEvent(args);
            foreach (var embeddedResourcesItem in this.EmbeddedResourcesItems)
            {
                if (String.Compare(Path.GetExtension(embeddedResourcesItem.ItemSpec),".resx",StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    this.BuildEngine.LogMessageEvent(
                       new BuildMessageEventArgs(
                           string.Format("Skipping converting non resx Resource file {0}.", embeddedResourcesItem.ItemSpec),
                           string.Empty,
                           "ResxToJs",
                           MessageImportance.Normal));
                    continue;
                }

                this.BuildEngine.LogMessageEvent(
                    new BuildMessageEventArgs(
                        string.Format("Started converting Resx {0}.", embeddedResourcesItem.ItemSpec),
                        string.Empty,
                        "ResxToJs",
                        MessageImportance.Normal));

                var outputFileName = Path.GetFileNameWithoutExtension(embeddedResourcesItem.ItemSpec) + ".js";
                if (!string.IsNullOrEmpty(this.AssemblyName.ItemSpec))
                {
                    outputFileName = this.AssemblyName.ItemSpec + "." + outputFileName;
                }

                var outputFilePath = Path.Combine(
                    outputFullPath,
                    outputFileName);

                var content = this.GetJsonContent(embeddedResourcesItem.ItemSpec);

                using (var file = new StreamWriter(outputFilePath))
                {
                    file.Write(content);
                }

                outputFileName = Path.GetFileNameWithoutExtension(embeddedResourcesItem.ItemSpec) + ".js";

                var resxFilePath = Path.Combine(this.ProjectPath, embeddedResourcesItem.ItemSpec);

                // make a copy in the project path
                var destinationFilePath = Path.Combine(Path.GetDirectoryName(resxFilePath), outputFileName);
                File.Copy(outputFilePath, destinationFilePath, true);

                this.BuildEngine.LogMessageEvent(
                    new BuildMessageEventArgs(
                        string.Format("Generated file {0}", outputFileName),
                        string.Empty,
                        "ResxToJs",
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

            var jsonName = Path.GetFileNameWithoutExtension(resourceItem);
            if (cultureInfo != null)
            {
                // this will get rid of locale from the resx name
                jsonName = Path.GetFileNameWithoutExtension(jsonName);
            }

            if (!string.IsNullOrEmpty(this.AssemblyName.ItemSpec))
            {
                jsonName = this.AssemblyName.ItemSpec + "." + jsonName;
            }

            var definingNameSpace = new StringBuilder();

            var namespaceParts = jsonName.Split('.');

            var incrementalNameSpace = string.Empty;

            var prefix = "var ";

            // leave the last part
            for (var i = 0; i < namespaceParts.Length - 1; i++)
            {                
                incrementalNameSpace += (string.IsNullOrEmpty(incrementalNameSpace) ? string.Empty : ".") + namespaceParts[i];
                definingNameSpace.AppendFormat("{0}{1} = {1}||{{}};\r\n", prefix, incrementalNameSpace);
                prefix = string.Empty;
            }	      
 
			 return string.Format(OutputJsonFormat, definingNameSpace.ToString(), jsonName, json);
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
            Dictionary<string, object> strings;
            using (var rsxr = new ResXResourceReader(resourceItem))
            {
                rsxr.UseResXDataNodes = true;
                strings = rsxr.Cast<DictionaryEntry>()
                    .ToDictionary(
                        x => x.Key.ToString(),
                        x => ((ResXDataNode)x.Value).GetValue((ITypeResolutionService)null));
            }

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