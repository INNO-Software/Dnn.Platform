#region Copyright
// 
// DotNetNuke� - http://www.dotnetnuke.com
// Copyright (c) 2002-2014
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion
#region Usings

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Content;
using DotNetNuke.Entities.Content.Common;
using DotNetNuke.Entities.Content.Taxonomy;
using DotNetNuke.Entities.Modules.Definitions;

#endregion

namespace DotNetNuke.Entities.Modules
{
    /// -----------------------------------------------------------------------------
    /// Project	 : DotNetNuke
    /// Namespace: DotNetNuke.Entities.Modules
    /// Class	 : DesktopModuleInfo
    /// -----------------------------------------------------------------------------
    /// <summary>
    /// DesktopModuleInfo provides the Entity Layer for Desktop Modules
    /// </summary>
    /// <history>
    /// 	[cnurse]	01/11/2008   Documented
    /// </history>
    /// -----------------------------------------------------------------------------
    [Serializable]
    public class DesktopModuleInfo : ContentItem, IXmlSerializable
    {
        private Dictionary<string, ModuleDefinitionInfo> _moduleDefinitions;

        public DesktopModuleInfo()
        {
            IsPremium = Null.NullBoolean;
            IsAdmin = Null.NullBoolean;
            CodeSubDirectory = Null.NullString;
            PackageID = Null.NullInteger;
            DesktopModuleID = Null.NullInteger;
            SupportedFeatures = Null.NullInteger;
            Shareable = ModuleSharing.Unknown;
        }

		#region "Public Properties"

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets the ID of the Desktop Module
        /// </summary>
        /// <returns>An Integer</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public int DesktopModuleID { get; set; }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets the ID of the Package for this Desktop Module
        /// </summary>
        /// <returns>An Integer</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public int PackageID { get; set; }

        /// <summary>
        /// returns whether this has an associated Admin page
        /// </summary>
        public string AdminPage { get; set; }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets the BusinessControllerClass of the Desktop Module
        /// </summary>
        /// <returns>A String</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public string BusinessControllerClass { get; set; }

        public string Category
        {
            get
            {
                Term term = (from Term t in Terms select t).FirstOrDefault();
                return (term != null) ? term.Name : String.Empty;
            } 
            set
            {
                Terms.Clear();
                ITermController termController = Util.GetTermController();
                var term = (from Term t in termController.GetTermsByVocabulary("Module_Categories") 
                            where t.Name == value 
                            select t)
                            .FirstOrDefault();
                if (term != null)
                {
                    Terms.Add(term);
                }
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets the AppCode Folder Name of the Desktop Module
        /// </summary>
        /// <returns>A String</returns>
        /// <history>
        /// 	[cnurse]	02/20/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public string CodeSubDirectory { get; set; }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets a Regular Expression that matches the versions of the core
        /// that this module is compatible with
        /// </summary>
        /// <returns>A String</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public string CompatibleVersions { get; set; }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets a list of Dependencies for the module
        /// </summary>
        /// <returns>A String</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public string Dependencies { get; set; }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets the  Description of the Desktop Module
        /// </summary>
        /// <returns>A String</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public string Description { get; set; }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets the Folder Name of the Desktop Module
        /// </summary>
        /// <returns>A String</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public string FolderName { get; set; }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets the Friendly Name of the Desktop Module
        /// </summary>
        /// <returns>A String</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public string FriendlyName { get; set; }

        /// <summary>
        /// returns whether this has an associated hostpage
        /// </summary>
        public string HostPage { get; set; }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets whether the Module is an Admin Module
        /// </summary>
        /// <returns>A Boolean</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public bool IsAdmin { get; set; }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets whether the Module is Portable
        /// </summary>
        /// <returns>A Boolean</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public bool IsPortable
        {
            get
            {
                return GetFeature(DesktopModuleSupportedFeature.IsPortable);
            }
            set
            {
                UpdateFeature(DesktopModuleSupportedFeature.IsPortable, value);
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets whether the Module is a Premium Module
        /// </summary>
        /// <returns>A Boolean</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public bool IsPremium { get; set; }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets whether the Module is Searchable
        /// </summary>
        /// <returns>A Boolean</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public bool IsSearchable
        {
            get
            {
                return GetFeature(DesktopModuleSupportedFeature.IsSearchable);
            }
            set
            {
                UpdateFeature(DesktopModuleSupportedFeature.IsSearchable, value);
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets whether the Module is Upgradable
        /// </summary>
        /// <returns>A Boolean</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public bool IsUpgradeable
        {
            get
            {
                return GetFeature(DesktopModuleSupportedFeature.IsUpgradeable);
            }
            set
            {
                UpdateFeature(DesktopModuleSupportedFeature.IsUpgradeable, value);
            }
        }

        /// <summary>
        /// Is the module allowed to be shared across sites?
        /// </summary>
        public ModuleSharing Shareable
        {
            get;
            set; 
        }
        /// <summary>
        /// Gets and sets the iconfile if this is defined in the module manifest
        /// </summary>
        public string TabIconFile { get; set; }
        /// <summary>
        /// Gets and sets the large iconfile if this is defined in the module manifest
        /// </summary>
        public string TabIconFileLarge { get; set; }
        /// <summary>
        /// Gets and sets the tab description if this is defined in the module manifest
        /// </summary>
        public string TabDescription { get; set; }
        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets the Module Definitions for this Desktop Module
        /// </summary>
        /// <returns>A Boolean</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public Dictionary<string, ModuleDefinitionInfo> ModuleDefinitions
        {
            get
            {
                if (_moduleDefinitions == null)
                {
                    if (DesktopModuleID > Null.NullInteger)
                    {
                        _moduleDefinitions = ModuleDefinitionController.GetModuleDefinitionsByDesktopModuleID(DesktopModuleID);
                    }
                    else
                    {
                        _moduleDefinitions = new Dictionary<string, ModuleDefinitionInfo>();
                    }
                }
                return _moduleDefinitions;
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets the  Name of the Desktop Module
        /// </summary>
        /// <returns>A String</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public string ModuleName { get; set; }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets a list of Permissions for the module
        /// </summary>
        /// <returns>A String</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public string Permissions { get; set; }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets the Supported Features of the Module
        /// </summary>
        /// <returns>An Integer</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public int SupportedFeatures { get; set; }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets and sets the Version of the Desktop Module
        /// </summary>
        /// <returns>A String</returns>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        public string Version { get; set; }

		#endregion

		#region IHydratable Members

		/// -----------------------------------------------------------------------------
        /// <summary>
        /// Fills a DesktopModuleInfo from a Data Reader
        /// </summary>
        /// <param name="dr">The Data Reader to use</param>
        /// <history>
        /// 	[cnurse]	01/11/2008   Created
        /// </history>
        /// -----------------------------------------------------------------------------
        public override void Fill(IDataReader dr)
        {
            DesktopModuleID = Null.SetNullInteger(dr["DesktopModuleID"]);
            PackageID = Null.SetNullInteger(dr["PackageID"]);
            ModuleName = Null.SetNullString(dr["ModuleName"]);
            FriendlyName = Null.SetNullString(dr["FriendlyName"]);
            Description = Null.SetNullString(dr["Description"]);
            FolderName = Null.SetNullString(dr["FolderName"]);
            Version = Null.SetNullString(dr["Version"]);
            Description = Null.SetNullString(dr["Description"]);
            IsPremium = Null.SetNullBoolean(dr["IsPremium"]);
            IsAdmin = Null.SetNullBoolean(dr["IsAdmin"]);
            BusinessControllerClass = Null.SetNullString(dr["BusinessControllerClass"]);
            SupportedFeatures = Null.SetNullInteger(dr["SupportedFeatures"]);
            CompatibleVersions = Null.SetNullString(dr["CompatibleVersions"]);
            Dependencies = Null.SetNullString(dr["Dependencies"]);
            Permissions = Null.SetNullString(dr["Permissions"]);
		    Shareable = (ModuleSharing)Null.SetNullInteger(dr["Shareable"]);
            AdminPage = Null.SetNullString(dr["AdminPage"]);
            HostPage = Null.SetNullString(dr["HostPage"]);
            //Call the base classes fill method to populate base class proeprties
            base.FillInternal(dr);
        }

        #endregion

        #region IXmlSerializable Members

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets an XmlSchema for the DesktopModule
        /// </summary>
        /// <history>
        /// 	[cnurse]	01/17/2008   Created
        /// </history>
        /// -----------------------------------------------------------------------------
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Reads a DesktopModuleInfo from an XmlReader
        /// </summary>
        /// <param name="reader">The XmlReader to use</param>
        /// <history>
        /// 	[cnurse]	01/17/2008   Created
        /// </history>
        /// -----------------------------------------------------------------------------
        public void ReadXml(XmlReader reader)
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    break;
                }
                if (reader.NodeType == XmlNodeType.Whitespace)
                {
                    continue;
                }
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "moduleDefinitions" && !reader.IsEmptyElement)
                {
                    ReadModuleDefinitions(reader);
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "supportedFeatures" && !reader.IsEmptyElement)
                {
                    ReadSupportedFeatures(reader);
                }
                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "shareable" && !reader.IsEmptyElement)
                {
                    ReadModuleSharing(reader);
                }
                else
                {
                    switch (reader.Name)
                    {
                        case "desktopModule":
                            break;
                        case "moduleName":
                            ModuleName = reader.ReadElementContentAsString();
                            break;
                        case "foldername":
                            FolderName = reader.ReadElementContentAsString();
                            break;
                        case "businessControllerClass":
                            BusinessControllerClass = reader.ReadElementContentAsString();
                            break;
                        case "codeSubDirectory":
                            CodeSubDirectory = reader.ReadElementContentAsString();
                            break;
                        case "adminPage":
                            AdminPage = reader.ReadElementContentAsString();
                            break;
                        case "hostPage":
                            HostPage = reader.ReadElementContentAsString();
                            break;
                        case "tabIconFile":
                            TabIconFile = reader.ReadElementContentAsString();
                            break;
                        case "tabIconFileLarge":
                            TabIconFileLarge = reader.ReadElementContentAsString();
                            break;
                        case "tabDescription":
                            TabDescription = reader.ReadElementContentAsString();
                            break;
                        case "isAdmin":
                            bool isAdmin;
                            Boolean.TryParse(reader.ReadElementContentAsString(), out isAdmin);
                            IsAdmin = isAdmin;
                            break;
                        case "isPremium":
                            bool isPremium;
                            Boolean.TryParse(reader.ReadElementContentAsString(), out isPremium);
                            IsPremium = isPremium;
                            break;
                        default:
                            if(reader.NodeType == XmlNodeType.Element && !String.IsNullOrEmpty(reader.Name))
                            {
                                reader.ReadElementContentAsString();
                            }
                            break;
                    }
                }
            }
        }
		
		/// -----------------------------------------------------------------------------
        /// <summary>
        /// Writes a DesktopModuleInfo to an XmlWriter
        /// </summary>
        /// <param name="writer">The XmlWriter to use</param>
        /// <history>
        /// 	[cnurse]	01/17/2008   Created
        /// </history>
        /// -----------------------------------------------------------------------------

        public void WriteXml(XmlWriter writer)
        {
            //Write start of main elemenst
            writer.WriteStartElement("desktopModule");

            //write out properties
            writer.WriteElementString("moduleName", ModuleName);
            writer.WriteElementString("foldername", FolderName);
            writer.WriteElementString("businessControllerClass", BusinessControllerClass);
            if (!string.IsNullOrEmpty(CodeSubDirectory))
            {
                writer.WriteElementString("codeSubDirectory", CodeSubDirectory);
            }
            if (!string.IsNullOrEmpty(AdminPage))
            {
                writer.WriteElementString("adminPage", AdminPage);
            }
            if (!string.IsNullOrEmpty(HostPage))
            {
                writer.WriteElementString("hostPage", HostPage);
            }
            if (!string.IsNullOrEmpty(TabIconFile))
            {
                writer.WriteElementString("tabIconFile", HostPage);
            }
            if (!string.IsNullOrEmpty(TabIconFileLarge))
            {
                writer.WriteElementString("tabIconFileLarge", HostPage);
            }

            if (!string.IsNullOrEmpty(TabDescription))
            {
                writer.WriteElementString("tabDescription", TabDescription);
            }
			
            //Write out Supported Features
            writer.WriteStartElement("supportedFeatures");
            if (IsPortable)
            {
                writer.WriteStartElement("supportedFeature");
                writer.WriteAttributeString("type", "Portable");
                writer.WriteEndElement();
            }
            if (IsSearchable)
            {
                writer.WriteStartElement("supportedFeature");
                writer.WriteAttributeString("type", "Searchable");
                writer.WriteEndElement();
            }
            if (IsUpgradeable)
            {
                writer.WriteStartElement("supportedFeature");
                writer.WriteAttributeString("type", "Upgradeable");
                writer.WriteEndElement();
            }

            //Write end of Supported Features
            writer.WriteEndElement();

            // Module sharing

            if(Shareable != ModuleSharing.Unknown)
            {
                writer.WriteStartElement("shareable");
                switch (Shareable)
                {
                    case ModuleSharing.Supported:
                        writer.WriteString("Supported");
                        break;
                    case ModuleSharing.Unsupported:
                        writer.WriteString("Unsupported");
                        break;
                }
                writer.WriteEndElement();
            }

            //Write start of Module Definitions
            writer.WriteStartElement("moduleDefinitions");

            //Iterate through definitions
            foreach (ModuleDefinitionInfo definition in ModuleDefinitions.Values)
            {
                definition.WriteXml(writer);
            }
            //Write end of Module Definitions
            writer.WriteEndElement();

            //Write end of main element
            writer.WriteEndElement();
        }

        #endregion

		#region "Private Helper Methods"

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Clears a Feature from the Features
        /// </summary>
        /// <param name="feature">The feature to Clear</param>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        private void ClearFeature(DesktopModuleSupportedFeature feature)
        {
			//And with the 1's complement of Feature to Clear the Feature flag
            SupportedFeatures = SupportedFeatures & ~((int) feature);
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Gets a Feature from the Features
        /// </summary>
        /// <param name="feature">The feature to Get</param>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        private bool GetFeature(DesktopModuleSupportedFeature feature)
        {
            return SupportedFeatures > Null.NullInteger && (SupportedFeatures & (int) feature) == (int) feature;
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Sets a Feature in the Features
        /// </summary>
        /// <param name="feature">The feature to Set</param>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        private void SetFeature(DesktopModuleSupportedFeature feature)
        {
            SupportedFeatures |= (int) feature;
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Updates a Feature in the Features
        /// </summary>
        /// <param name="feature">The feature to Set</param>
        /// <param name="isSet">A Boolean indicating whether to set or clear the feature</param>
        /// <history>
        /// 	[cnurse]	01/11/2008   Documented
        /// </history>
        /// -----------------------------------------------------------------------------
        private void UpdateFeature(DesktopModuleSupportedFeature feature, bool isSet)
        {
            if (isSet)
            {
                SetFeature(feature);
            }
            else
            {
                ClearFeature(feature);
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Reads a Supported Features from an XmlReader
        /// </summary>
        /// <param name="reader">The XmlReader to use</param>
        /// <history>
        /// 	[cnurse]	01/17/2008   Created
        /// </history>
        /// -----------------------------------------------------------------------------
        private void ReadSupportedFeatures(XmlReader reader)
        {
            SupportedFeatures = 0;
            reader.ReadStartElement("supportedFeatures");
            do
            {
                if (reader.HasAttributes)
                {
                    reader.MoveToFirstAttribute();
                    switch (reader.ReadContentAsString())
                    {
                        case "Portable":
                            IsPortable = true;
                            break;
                        case "Searchable":
                            IsSearchable = true;
                            break;
                        case "Upgradeable":
                            IsUpgradeable = true;
                            break;
                    }
                }
            } while (reader.ReadToNextSibling("supportedFeature"));
        }

        private void ReadModuleSharing(XmlReader reader)
        {
            var sharing = reader.ReadElementString("shareable");

            if (string.IsNullOrEmpty(sharing))
            {
                Shareable = ModuleSharing.Unknown;
            }
            else
            {
                switch (sharing.ToLowerInvariant())
                {
                    case "supported":
                        Shareable = ModuleSharing.Supported;
                        break;
                    case "unsupported":
                        Shareable = ModuleSharing.Unsupported;
                        break;
                    default:
                    case "unknown":
                        Shareable = ModuleSharing.Unknown;
                        break;
                }
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Reads a Module Definitions from an XmlReader
        /// </summary>
        /// <param name="reader">The XmlReader to use</param>
        /// <history>
        /// 	[cnurse]	01/17/2008   Created
        /// </history>
        /// -----------------------------------------------------------------------------
        private void ReadModuleDefinitions(XmlReader reader)
        {
            reader.ReadStartElement("moduleDefinitions");
            do
            {
                reader.ReadStartElement("moduleDefinition");

				//Create new ModuleDefinition object
                var moduleDefinition = new ModuleDefinitionInfo();

				//Load it from the Xml
                moduleDefinition.ReadXml(reader);

				//Add to the collection
                ModuleDefinitions.Add(moduleDefinition.FriendlyName, moduleDefinition);
            } while (reader.ReadToNextSibling("moduleDefinition"));
		}

		#endregion
	}
}
