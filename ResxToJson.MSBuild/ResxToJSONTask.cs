namespace ResxToJson.MSBuild
{
    using System.Collections;
    using System.IO;
    using System.Linq;

    using Microsoft.Build.Framework;
    using System;
    using System.Resources;
    using System.Web.Script.Serialization;

    using Microsoft.SqlServer.Server;

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
        /// Gets list of JSON files created
        /// </summary>
        [Output]
        public ITaskItem[] JsonItems { get; private set; }

        /// <summary>
        /// Executes the Task
        /// </summary>
        /// <returns>True if success</returns>
        public bool Execute()
        {
            var args = new BuildMessageEventArgs(
                "Started converting Resx To JSON",
                string.Empty,
                "ResxToJson",
                MessageImportance.Normal);

            this.BuildEngine.LogMessageEvent(args);

            foreach (var embeddedResourcesItem in this.EmbeddedResourcesItems)
            {
                this.BuildEngine.LogMessageEvent(
                    new BuildMessageEventArgs(
                        string.Format("Started converting Resx {0}", embeddedResourcesItem.ItemSpec),
                        string.Empty,
                        "ResxToJson",
                        MessageImportance.Normal));
                using (var rsxr = new ResXResourceReader(embeddedResourcesItem.ItemSpec))
                {
                    rsxr.UseResXDataNodes = false;
                    var strings = rsxr.Cast<DictionaryEntry>()
                        .ToDictionary(x => x.Key.ToString(), x => x.Value.ToString());

                    var json = new JavaScriptSerializer().Serialize(strings);
                    var file =
                        new StreamWriter(Path.GetFileNameWithoutExtension(embeddedResourcesItem.ItemSpec) + ".json");
                    file.Write(json);
                    file.Close();
                }
            }

            return true;
        }
    }
}
