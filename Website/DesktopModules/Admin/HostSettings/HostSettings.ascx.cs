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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web.UI.WebControls;
using System.Xml;
using System.Xml.XPath;

using DotNetNuke.Application;
using DotNetNuke.Common;
using DotNetNuke.Common.Lists;
using DotNetNuke.Common.Utilities;
using DotNetNuke.ComponentModel;
using DotNetNuke.Data;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Urls;
using DotNetNuke.Framework;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Framework.Providers;
using DotNetNuke.Security;
using DotNetNuke.Security.Membership;
using DotNetNuke.Services.Cache;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Installer;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.Log.EventLog;
using DotNetNuke.Services.Mail;
using DotNetNuke.Services.ModuleCache;
using DotNetNuke.Services.OutputCache;
using DotNetNuke.Services.Scheduling;
using DotNetNuke.Services.Upgrade;
using DotNetNuke.UI.Skins;
using DotNetNuke.UI.Skins.Controls;
using DotNetNuke.UI.WebControls;
using DotNetNuke.Web.Client.ClientResourceManagement;
using DotNetNuke.Web.UI.WebControls;
using DotNetNuke.Web.UI.WebControls.Extensions;
using OAuth.AuthorizationServer.Core.Server;

#endregion

namespace DotNetNuke.Modules.Admin.Host
{
    using System.Globalization;
    using System.Web;
    using Web.Client;
    using DotNetNuke.Services.Search.Internals;

    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The HostSettings PortalModuleBase is used to edit the host settings
    /// for the application.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// -----------------------------------------------------------------------------
    public partial class HostSettings : PortalModuleBase
	{
		#region Fields

		//Field Boost Settings - they are scaled down by 10.
		internal const int DefaultSearchTitleBoost = 50;
		internal const int DefaultSearchTagBoost = 40;
		internal const int DefaultSearchContentBoost = 35;
		internal const int DefaultSearchDescriptionBoost = 20;
		internal const int DefaultSearchAuthorBoost = 15;

		//Field Bosst Setting Names
		internal const string SearchTitleBoostSetting = "Search_Title_Boost";
		internal const string SearchTagBoostSetting = "Search_Tag_Boost";
		internal const string SearchContentBoostSetting = "Search_Content_Boost";
		internal const string SearchDescriptionBoostSetting = "Search_Description_Boost";
		internal const string SearchAuthorBoostSetting = "Search_Author_Boost";

		#endregion

		#region Private Methods

		/// -----------------------------------------------------------------------------
        /// <summary>
        /// BindData fetches the data from the database and updates the controls
        /// </summary>
        /// <history>
        /// 	[cnurse]	9/27/2004	Updated to reflect design changes for Help, 508 support
        ///                       and localisation
        /// </history>
        /// -----------------------------------------------------------------------------
        private void BindConfiguration()
        {
            lblProduct.Text = DotNetNukeContext.Current.Application.Description;
            lblVersion.Text = Globals.FormatVersion(DotNetNukeContext.Current.Application.Version, true);

            betaRow.Visible = (DotNetNukeContext.Current.Application.Status != ReleaseMode.Stable);
            chkBetaNotice.Checked = Entities.Host.Host.DisplayBetaNotice;

            chkUpgrade.Checked = Entities.Host.Host.CheckUpgrade;
            hypUpgrade.ImageUrl = Upgrade.UpgradeIndicator(DotNetNukeContext.Current.Application.Version, Request.IsLocal, Request.IsSecureConnection);
            if (String.IsNullOrEmpty(hypUpgrade.ImageUrl))
            {
                hypUpgrade.Visible = false;
            }
            else
            {
                hypUpgrade.NavigateUrl = Upgrade.UpgradeRedirect();
            }
            lblDataProvider.Text = ProviderConfiguration.GetProviderConfiguration("data").DefaultProvider;
            lblFramework.Text = Globals.NETFrameworkVersion.ToString(2);

            if (WindowsIdentity.GetCurrent() != null)
            {
                // ReSharper disable PossibleNullReferenceException
                lblIdentity.Text = WindowsIdentity.GetCurrent().Name;
                // ReSharper restore PossibleNullReferenceException
            }
            lblHostName.Text = Dns.GetHostName();
            lblIPAddress.Text = Dns.GetHostEntry(lblHostName.Text).AddressList[0].ToString();
            lblPermissions.Text = SecurityPolicy.Permissions;
            if (string.IsNullOrEmpty(lblPermissions.Text))
            {
                lblPermissions.Text = Localization.GetString("None", LocalResourceFile);
            }
            lblApplicationPath.Text = string.IsNullOrEmpty(Globals.ApplicationPath) ? "/" : Globals.ApplicationPath;
            lblApplicationMapPath.Text = Globals.ApplicationMapPath;
            lblServerTime.Text = DateTime.Now.ToString();
            lblGUID.Text = Entities.Host.Host.GUID;
            chkWebFarm.Checked = CachingProvider.Instance().IsWebFarm();

        }

        private void BindFriendlyUrlsRequestFilters()
        {
            FriendlyUrlsExtensionControl.BindAction(-1, -1, -1);
            chkEnableRequestFilters.Checked = Entities.Host.Host.EnableRequestFilters;
        }

        private void BindHostDetails()
        {
            hostPortalsCombo.DataSource = PortalController.Instance.GetPortals();
            hostPortalsCombo.DataBind(Entities.Host.Host.HostPortalID.ToString());

            txtHostTitle.Text = Entities.Host.Host.HostTitle;
            txtHostURL.Text = Entities.Host.Host.HostURL;
            txtHostEmail.Text = Entities.Host.Host.HostEmail;
            valHostEmail.ValidationExpression = Globals.glbEmailRegEx;

            //Load DocTypes
            var docTypes = new Dictionary<string, string>
                               {
                                   { "0", string.IsNullOrEmpty(LocalizeString("LegacyDoctype")) ? "Legacy" : LocalizeString("LegacyDoctype") }, 
                                   { "1", string.IsNullOrEmpty(LocalizeString("TransDoctype")) ? "Trans" : LocalizeString("TransDoctype") }, 
                                   { "2", string.IsNullOrEmpty(LocalizeString("StrictDoctype")) ? "Strict" : LocalizeString("StrictDoctype") },
                                   { "3", string.IsNullOrEmpty(LocalizeString("Html5Doctype")) ? "Html5" : LocalizeString("Html5Doctype") }
                               };

            docTypeCombo.DataSource = docTypes;
            docTypeCombo.DataBind();

            string docTypesetting = string.Empty;
            if (Globals.DataBaseVersion != null)
            {
                HostController.Instance.GetSettingsDictionary().TryGetValue("DefaultDocType", out docTypesetting);
            }
            if (string.IsNullOrEmpty(docTypesetting))
            {
                docTypesetting = "0";
            }
            docTypeCombo.DataBind(docTypesetting);

            chkRemember.Checked = Entities.Host.Host.RememberCheckbox;

            chkUpgradeForceSSL.Checked = Entities.Host.Host.UpgradeForceSsl;
            txtSSLDomain.Text = Entities.Host.Host.SslDomain;
        }

        private void BindJQuery()
        {
            jQueryVersion.Text = Framework.jQuery.Version;
            jQueryUIVersion.Text = Framework.jQuery.UIVersion;            
        }

		private void BindCdnSettings()
		{
			chkMsAjaxCdn.Checked = Entities.Host.Host.EnableMsAjaxCdn;
			chkTelerikCdn.Checked = Entities.Host.Host.EnableTelerikCdn;
			txtTelerikBasicUrl.Text = Entities.Host.Host.TelerikCdnBasicUrl;
			txtTelerikSecureUrl.Text = Entities.Host.Host.TelerikCdnSecureUrl;
		    chkEnableCDN.Checked = Entities.Host.Host.CdnEnabled;
		}

        private void BindPerformance()
        {
            cboPageState.Items.FindByValue(Entities.Host.Host.PageStatePersister).Selected = true; 
            BindModuleCacheProviderList();
            BindPageCacheProviderList();
            if (cboPerformance.FindItemByValue(((int)Entities.Host.Host.PerformanceSetting).ToString()) != null)
            {
                cboPerformance.FindItemByValue(((int)Entities.Host.Host.PerformanceSetting).ToString()).Selected = true;
            }
            else
            {
                cboPerformance.FindItemByValue("3").Selected = true;
            }
            cboCacheability.FindItemByValue(Entities.Host.Host.AuthenticatedCacheability).Selected = true;
        }

        private void BindPaymentProcessor()
        {
            var listController = new ListController();
            processorCombo.DataSource = listController.GetListEntryInfoItems("Processor", "");
            processorCombo.DataBind();
            processorCombo.InsertItem(0, "<" + Localization.GetString("None_Specified") + ">", "");
            processorCombo.Select(Entities.Host.Host.PaymentProcessor, true);

            processorLink.NavigateUrl = Globals.AddHTTP(processorCombo.SelectedItem.Value);

            txtUserId.Text = Entities.Host.Host.ProcessorUserId;
            txtPassword.Attributes.Add("value", Entities.Host.Host.ProcessorPassword);
            txtHostFee.Text = Entities.Host.Host.HostFee.ToString();

            currencyCombo.DataSource = listController.GetListEntryInfoItems("Currency", "");
            var currency = Entities.Host.Host.HostCurrency;
            if (String.IsNullOrEmpty(currency))
            {
                currency = "USD";
            }
            currencyCombo.DataBind(currency);

            txtHostSpace.Text = Entities.Host.Host.HostSpace.ToString();
            txtPageQuota.Text = Entities.Host.Host.PageQuota.ToString();
            txtUserQuota.Text = Entities.Host.Host.UserQuota.ToString();

            txtDemoPeriod.Text = Entities.Host.Host.DemoPeriod.ToString();
            chkDemoSignup.Checked = Entities.Host.Host.DemoSignup;
        }

        private void BindProxyServer()
        {
            txtProxyServer.Text = Entities.Host.Host.ProxyServer;
            txtProxyPort.Text = Entities.Host.Host.ProxyPort.ToString();
            txtProxyUsername.Text = Entities.Host.Host.ProxyUsername;
            txtProxyPassword.Attributes.Add("value", Entities.Host.Host.ProxyPassword);
            txtWebRequestTimeout.Text = Entities.Host.Host.WebRequestTimeout.ToString();
        }

        private void BindSkins()
        {
            hostSkinCombo.RootPath = SkinController.RootSkin;
            hostSkinCombo.Scope = SkinScope.Host;
            hostSkinCombo.SelectedValue = Entities.Host.Host.DefaultPortalSkin;

            hostContainerCombo.RootPath = SkinController.RootContainer;
            hostContainerCombo.Scope = SkinScope.Host;
            hostContainerCombo.SelectedValue = Entities.Host.Host.DefaultPortalContainer;

            editSkinCombo.RootPath = SkinController.RootSkin;
            editSkinCombo.Scope = SkinScope.Host;
            editSkinCombo.SelectedValue = Entities.Host.Host.DefaultAdminSkin;

            editContainerCombo.RootPath = SkinController.RootContainer;
            editContainerCombo.Scope = SkinScope.Host;
            editContainerCombo.SelectedValue = Entities.Host.Host.DefaultAdminContainer;

            uploadSkinLink.NavigateUrl = Util.InstallURL(ModuleContext.TabId, "");

            if (PortalSettings.EnablePopUps)
            {
                uploadSkinLink.Attributes.Add("onclick", "return " + UrlUtils.PopUpUrl(uploadSkinLink.NavigateUrl, this, PortalSettings, true, false));
            }
        }

        private void BindSmtpServer()
        {
            txtSMTPServer.Text = Entities.Host.Host.SMTPServer;
            txtConnectionLimit.Text = Entities.Host.Host.SMTPConnectionLimit.ToString(CultureInfo.InvariantCulture);
            txtMaxIdleTime.Text = Entities.Host.Host.SMTPMaxIdleTime.ToString(CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(Entities.Host.Host.SMTPAuthentication))
            {
                optSMTPAuthentication.Items.FindByValue(Entities.Host.Host.SMTPAuthentication).Selected = true;
            }
            else
            {
                optSMTPAuthentication.Items.FindByValue("0").Selected = true;
            }
            chkSMTPEnableSSL.Checked = Entities.Host.Host.EnableSMTPSSL;
            txtSMTPUsername.Text = Entities.Host.Host.SMTPUsername;
            txtSMTPPassword.Attributes.Add("value", Entities.Host.Host.SMTPPassword);
        }

        private void BindUpgradeLogs()
        {
            ProviderConfiguration objProviderConfiguration = ProviderConfiguration.GetProviderConfiguration("data");
            string strProviderPath = DataProvider.Instance().GetProviderPath();
            var arrScriptFiles = new ArrayList();
            string[] arrFiles = Directory.GetFiles(strProviderPath, "*." + objProviderConfiguration.DefaultProvider);
            foreach (string strFile in arrFiles)
            {
                arrScriptFiles.Add(Path.GetFileNameWithoutExtension(strFile));
            }
            arrScriptFiles.Sort();
            cboVersion.DataSource = arrScriptFiles;
            cboVersion.DataBind();
        }

        private void BindData()
        {
            BindConfiguration();
            BindHostDetails();
            chkCopyright.Checked = Entities.Host.Host.DisplayCopyright;
            chkUseCustomErrorMessages.Checked = Entities.Host.Host.UseCustomErrorMessages;
            chkUseCustomModuleCssClass.Checked = Entities.Host.Host.EnableCustomModuleCssClass;
            BindSkins();
            BindPaymentProcessor();
            BindFriendlyUrlsRequestFilters();
            BindProxyServer();
            BindSmtpServer();
            BindPerformance();
            BindJQuery();
	        BindCdnSettings();
            BindClientResourceManagement();
            BindLogList();
            BindIpFilters();
            ManageMinificationUi();

            foreach (KeyValuePair<string, ModuleControlInfo> kvp in ModuleControlController.GetModuleControlsByModuleDefinitionID(Null.NullInteger))
            {
                if (kvp.Value.ControlType == SecurityAccessLevel.ControlPanel)
                {
                    cboControlPanel.AddItem(kvp.Value.ControlKey.Replace("CONTROLPANEL:", ""), kvp.Value.ControlSrc);
                }
            }
            if (string.IsNullOrEmpty(Entities.Host.Host.ControlPanel))
            {
                if (cboControlPanel.FindItemByValue(Globals.glbDefaultControlPanel) != null)
                {
                    cboControlPanel.FindItemByValue(Globals.glbDefaultControlPanel).Selected = true;
                }
            }
            else
            {
                if (cboControlPanel.FindItemByValue(Entities.Host.Host.ControlPanel) != null)
                {
                    cboControlPanel.FindItemByValue(Entities.Host.Host.ControlPanel).Selected = true;
                }
            }

            chkUsersOnline.Checked = Entities.Host.Host.EnableUsersOnline;
            txtUsersOnlineTime.Text = Entities.Host.Host.UsersOnlineTimeWindow.ToString();
            txtAutoAccountUnlock.Text = Entities.Host.Host.AutoAccountUnlockDuration.ToString();

            txtFileExtensions.Text = Entities.Host.Host.AllowedExtensionWhitelist.ToStorageString();

            chkLogBuffer.Checked = Entities.Host.Host.EventLogBuffer;
            txtHelpURL.Text = Entities.Host.Host.HelpURL;
            chkEnableHelp.Checked = Entities.Host.Host.EnableModuleOnLineHelp;
            chkAutoSync.Checked = Entities.Host.Host.EnableFileAutoSync;
            chkEnableContentLocalization.Checked = Entities.Host.Host.EnableContentLocalization;
            chkDebugMode.Checked = Entities.Host.Host.DebugMode;
            chkCriticalErrors.Checked = Entities.Host.Host.ShowCriticalErrors;
            chkEnableOAuth.Checked = Entities.Host.Host.EnableOAuthAuthorization;
            txtBatch.Text = Entities.Host.Host.MessageSchedulerBatchSize.ToString();
            txtMaxUploadSize.Text = (Config.GetMaxUploadSize() / (1024 * 1024)).ToString();
			txtAsyncTimeout.Text = Entities.Host.Host.AsyncTimeout.ToString();

            chkBannedList.Checked = Entities.Host.Host.EnableBannedList;
            chkStrengthMeter.Checked = Entities.Host.Host.EnableStrengthMeter;
            chkIPChecking.Checked = Entities.Host.Host.EnableIPChecking;
            chkEnablePasswordHistory.Checked = Entities.Host.Host.EnablePasswordHistory;
            txtResetLinkValidity.Text = Entities.Host.Host.MembershipResetLinkValidity.ToString();
            txtAdminResetLinkValidity.Text = Entities.Host.Host.AdminMembershipResetLinkValidity.ToString();
            txtNumberPasswords.Text = Entities.Host.Host.MembershipNumberPasswords.ToString();
           
            ViewState["SelectedLogBufferEnabled"] = chkLogBuffer.Checked;
            ViewState["SelectedUsersOnlineEnabled"] = chkUsersOnline.Checked;

            BindUpgradeLogs();
        }

        private void BindLogList()
        {
            var files = Directory.GetFiles(Globals.ApplicationMapPath + @"\portals\_default\logs", "*.resources");
            IEnumerable<string> fileList = (from file in files select Path.GetFileName(file));
            ddlLogs.DataSource = fileList;
            ddlLogs.DataBind();
            var selectItem = new ListItem(Localization.GetString("SelectLog", LocalResourceFile), "-1");
            ddlLogs.InsertItem(0, selectItem.Text, selectItem.Value);
            ddlLogs.SelectedIndex = 0;
        }

        private void BindClientResourceManagement()
        {
            DebugEnabledRow.Visible = HttpContext.Current.IsDebuggingEnabled;
            CrmVersion.Text = Entities.Host.Host.CrmVersion.ToString(CultureInfo.InvariantCulture);
            chkCrmEnableCompositeFiles.Checked = Entities.Host.Host.CrmEnableCompositeFiles;
            chkCrmMinifyCss.Checked = Entities.Host.Host.CrmMinifyCss;
            chkCrmMinifyJs.Checked = Entities.Host.Host.CrmMinifyJs;
        }

        private void BindIpFilters()
        {
            divFiltersDisabled.Visible = !Entities.Host.Host.EnableIPChecking;
        }

        private void BindModuleCacheProviderList()
        {
            cboModuleCacheProvider.DataSource = GetFilteredProviders(ModuleCachingProvider.GetProviderList(), "ModuleCachingProvider");
            cboModuleCacheProvider.DataBind();
            if (cboModuleCacheProvider.Items.Count > 0)
            {
                var defaultModuleCache = ComponentFactory.GetComponent<ModuleCachingProvider>();
                string providerKey = (from provider in ModuleCachingProvider.GetProviderList() where provider.Value.Equals(defaultModuleCache) select provider.Key).SingleOrDefault();
                if (!string.IsNullOrEmpty(Entities.Host.Host.ModuleCachingMethod))
                {
                    if (cboModuleCacheProvider.FindItemByValue(Entities.Host.Host.ModuleCachingMethod) != null)
                    {
                        cboModuleCacheProvider.FindItemByValue(Entities.Host.Host.ModuleCachingMethod).Selected = true;
                    }
                    else
                    {
                        cboModuleCacheProvider.FindItemByValue(providerKey).Selected = true;
                    }
                }
                else
                {
                    cboModuleCacheProvider.FindItemByValue(providerKey).Selected = true;
                }
            }
        }

        private void BindPageCacheProviderList()
        {
            cboPageCacheProvider.DataSource = GetFilteredProviders(OutputCachingProvider.GetProviderList(), "OutputCachingProvider");
            cboPageCacheProvider.DataBind();
            if (cboPageCacheProvider.Items.Count > 0)
            {
                var defaultPageCache = ComponentFactory.GetComponent<OutputCachingProvider>();
                var providerKey = (from provider in OutputCachingProvider.GetProviderList() where provider.Value.Equals(defaultPageCache) select provider.Key).SingleOrDefault();
                if (defaultPageCache != null)
                {
                    PageCacheRow.Visible = true;
                    if (!string.IsNullOrEmpty(Entities.Host.Host.PageCachingMethod))
                    {
                        if (cboPageCacheProvider.FindItemByValue(Entities.Host.Host.PageCachingMethod) != null)
                        {
                            cboPageCacheProvider.FindItemByValue(Entities.Host.Host.PageCachingMethod).Selected = true;
                        }
                        else
                        {
                            cboPageCacheProvider.FindItemByValue(providerKey).Selected = true;
                        }
                    }
                    else
                    {
                        cboPageCacheProvider.FindItemByValue(providerKey).Selected = true;
                    }
                }
            }
            else
            {
                PageCacheRow.Visible = false;
            }
        }

        private void CheckSecurity()
        {
			//Verify that the current user has access to access this page
            if (!UserInfo.IsSuperUser)
            {
                Response.Redirect(Globals.NavigateURL("Access Denied"), true);
            }
        }

        private static IEnumerable GetFilteredProviders<T>(Dictionary<string, T> providerList, string keyFilter)
        {
            var providers = from provider in providerList let filteredkey = provider.Key.Replace(keyFilter, String.Empty) select new { filteredkey, provider.Key };

            return providers;
        }

        private void OnLogFileIndexChanged(object sender, EventArgs e)
        {
            if (ddlLogs.SelectedItem.Value == "-1")
            {
                txtLogContents.Text = string.Empty;
                txtLogContents.Visible = false;
                return;
            }

            var objStreamReader = File.OpenText(Globals.ApplicationMapPath + @"\portals\_default\logs\" + ddlLogs.SelectedItem.Text);
            var logText = objStreamReader.ReadToEnd();
            if (String.IsNullOrEmpty(logText.Trim()))
            {
                logText = Localization.GetString("LogEmpty", LocalResourceFile);
            }
            txtLogContents.Text = logText;
            txtLogContents.Visible = true;
            objStreamReader.Close();
        }
        #endregion

        #region Protected Methods

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
			JavaScript.RequestRegistration(CommonJs.DnnPlugins);
            ddlLogs.SelectedIndexChanged += OnLogFileIndexChanged;
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// Page_Load runs when the control is loaded.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        /// 	[cnurse]	9/27/2004	Updated to reflect design changes for Help, 508 support
        ///                       and localisation
        ///     [VMasanas]  9/28/2004   Changed redirect to Access Denied
        /// </history>
        /// -----------------------------------------------------------------------------
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            cmdEmail.Click += TestEmail;
            cmdRestart.Click += RestartApplication;
            cmdUpdate.Click += UpdateSettings;
            cmdUpgrade.Click += OnUpgradeClick;
            cmdCache.Click += ClearCache;
            IncrementCrmVersionButton.Click += IncrementCrmVersion;
            chkCrmEnableCompositeFiles.CheckedChanged += EnableCompositeFilesChanged;
            try
            {
                CheckSecurity();

                //If this is the first visit to the page, populate the site data
                if (!Page.IsPostBack)
                {
                    BindData();
                    BindSearchIndex();
                   
                    rangeUploadSize.MaximumValue = Config.GetRequestFilterSize().ToString();
                    rangeUploadSize.Text = String.Format(Localization.GetString("maxUploadSize.Error", LocalResourceFile),rangeUploadSize.MaximumValue);
                    rangeUploadSize.ErrorMessage = String.Format(Localization.GetString("maxUploadSize.Error", LocalResourceFile), rangeUploadSize.MaximumValue);

                    if(Request.QueryString["smtpwarning"] != null)
                    {
                        Skin.AddModuleMessage(this, Localization.GetString("SmtpServerWarning", LocalResourceFile), ModuleMessage.ModuleMessageType.YellowWarning);
                    }
                }
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
            passwordSettings.EditMode = PropertyEditorMode.Edit ;
            passwordSettings.LocalResourceFile = LocalResourceFile;
            passwordSettings.DataSource = new PasswordConfig();
            passwordSettings.DataBind();
        }

        private void BindSearchIndex()
        {
            var folder = HostController.Instance.GetString("SearchFolder", @"App_Data\Search");
            var indexFolder = Path.Combine(Globals.ApplicationMapPath, folder);
            lblSearchIndexPath.Text = indexFolder;

            var minWordLength = HostController.Instance.GetInteger("Search_MinKeyWordLength", 3);
            var maxWordLength = HostController.Instance.GetInteger("Search_MaxKeyWordLength", 255);
            txtIndexWordMinLength.Text = minWordLength.ToString(CultureInfo.InvariantCulture);
            txtIndexWordMaxLength.Text = maxWordLength.ToString(CultureInfo.InvariantCulture);
			chkAllowLeadingWildcard.Checked = HostController.Instance.GetString("Search_AllowLeadingWildcard", "N") == "Y";

            var noneSpecified = "<" + Localization.GetString("None_Specified") + ">";

            cbCustomAnalyzer.DataSource = GetAvailableAnalyzers();
            cbCustomAnalyzer.DataBind();
            cbCustomAnalyzer.Items.Insert(0, new DnnComboBoxItem(noneSpecified, string.Empty));
            cbCustomAnalyzer.Select(HostController.Instance.GetString("Search_CustomAnalyzer", string.Empty), false);

			txtTitleBoost.Text = HostController.Instance.GetInteger(SearchTitleBoostSetting, DefaultSearchTitleBoost).ToString();
			txtTagBoost.Text = HostController.Instance.GetInteger(SearchTagBoostSetting, DefaultSearchTagBoost).ToString();
			txtContentBoost.Text = HostController.Instance.GetInteger(SearchContentBoostSetting, DefaultSearchContentBoost).ToString();
			txtDescriptionBoost.Text = HostController.Instance.GetInteger(SearchDescriptionBoostSetting, DefaultSearchDescriptionBoost).ToString();
			txtAuthorBoost.Text = HostController.Instance.GetInteger(SearchAuthorBoostSetting, DefaultSearchAuthorBoost).ToString();
        }

        private IList<string> GetAvailableAnalyzers()
        {
            var analyzers = new List<string>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    analyzers.AddRange(from t in assembly.GetTypes() where IsAnalyzerType(t) && IsAllowType(t) select string.Format("{0}, {1}", t.FullName, assembly.GetName().Name));
                }
                catch (Exception)
                {
                    //do nothing but just ignore the error.
                }
                    
            }


            return analyzers;
        }

        private bool IsAnalyzerType(Type type)
        {
            return type != null && type.FullName != null && (type.FullName.Contains("Lucene.Net.Analysis.Analyzer") || IsAnalyzerType(type.BaseType));
        }

        private bool IsAllowType(Type type)
        {
            return !type.FullName.Contains("Lucene.Net.Analysis.Analyzer") && !type.FullName.Contains("DotNetNuke");
        }

        private void EnableCompositeFilesChanged(object sender, EventArgs e)
        {
            ManageMinificationUi();
        }

        private void ManageMinificationUi()
        {
            var enableCompositeFiles = chkCrmEnableCompositeFiles.Checked;

            if (!enableCompositeFiles)
            {
                chkCrmMinifyCss.Checked = false;
                chkCrmMinifyJs.Checked = false;
            }

            chkCrmMinifyCss.Enabled = enableCompositeFiles;
            chkCrmMinifyJs.Enabled = enableCompositeFiles;
        }

        private void IncrementCrmVersion(object sender, EventArgs e)
        {
            var currentVersion = Entities.Host.Host.CrmVersion;
            var newVersion = currentVersion + 1;
            HostController.Instance.Update(ClientResourceSettings.VersionKey, newVersion.ToString(CultureInfo.InvariantCulture), true);
            Response.Redirect(Request.RawUrl, true); // reload page
        }

        protected void OnUpgradeClick(object sender, EventArgs e)
        {
            try
            {
                var strProviderPath = DataProvider.Instance().GetProviderPath();
                if (File.Exists(strProviderPath + cboVersion.SelectedItem.Text + ".log.resources"))
                {
                    var objStreamReader = File.OpenText(strProviderPath + cboVersion.SelectedItem.Text + ".log.resources");
                    var upgradeText = objStreamReader.ReadToEnd();
                    if (String.IsNullOrEmpty(upgradeText.Trim()))
                    {
                        upgradeText = Localization.GetString("LogEmpty", LocalResourceFile);
                    }
                    lblUpgrade.Text = upgradeText.Replace("\n", "<br>");
                    objStreamReader.Close();
                }
                else
                {
                    lblUpgrade.Text = Localization.GetString("NoLog", LocalResourceFile);
                }
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// ClearCache runs when the clear cache button is clicked
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        /// 	[cnurse]	9/27/2004	Updated to reflect design changes for Help, 508 support
        ///                       and localisation
        /// </history>
        /// -----------------------------------------------------------------------------
        protected void ClearCache(object sender, EventArgs e)
        {
            DataCache.ClearCache();
			ClientResourceManager.ClearCache();
            Response.Redirect(Request.RawUrl, true);
        }

        protected void RestartApplication(object sender, EventArgs e)
        {
            var log = new LogInfo { BypassBuffering = true, LogTypeKey = EventLogController.EventLogType.HOST_ALERT.ToString() };
            log.AddProperty("Message", Localization.GetString("UserRestart", LocalResourceFile));
            LogController.Instance.AddLog(log);
            Config.Touch();
            Response.Redirect(Globals.NavigateURL(), true);
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// TestEmail runs when the test email button is clicked
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        /// 	[cnurse]	9/27/2004	Updated to reflect design changes for Help, 508 support
        ///                       and localisation
        /// </history>
        /// -----------------------------------------------------------------------------
        protected void TestEmail(object sender, EventArgs e)
        {
            try
            {
                if (!String.IsNullOrEmpty(txtHostEmail.Text))
                {
                    txtSMTPPassword.Attributes.Add("value", txtSMTPPassword.Text);

                    string strMessage = Mail.SendMail(txtHostEmail.Text,
                                                      txtHostEmail.Text,
                                                      "",
                                                      "",
                                                      MailPriority.Normal,
                                                      Localization.GetSystemMessage(PortalSettings, "EMAIL_SMTP_TEST_SUBJECT"),
                                                      MailFormat.Text,
                                                      Encoding.UTF8,
                                                      "",
                                                      "",
                                                      txtSMTPServer.Text,
                                                      optSMTPAuthentication.SelectedItem.Value,
                                                      txtSMTPUsername.Text,
                                                      txtSMTPPassword.Text,
                                                      chkSMTPEnableSSL.Checked);
                    if (!String.IsNullOrEmpty(strMessage))
                    {
                        UI.Skins.Skin.AddModuleMessage(this, "", String.Format(Localization.GetString("EmailErrorMessage", LocalResourceFile), strMessage), ModuleMessage.ModuleMessageType.RedError);
                    }
                    else
                    {
                        UI.Skins.Skin.AddModuleMessage(this, "", String.Format(Localization.GetString("EmailSentMessage", LocalResourceFile), txtHostEmail.Text), ModuleMessage.ModuleMessageType.GreenSuccess);
                    }
                }
                else
                {
                    UI.Skins.Skin.AddModuleMessage(this, "", Localization.GetString("SpecifyHostEmailMessage", LocalResourceFile), ModuleMessage.ModuleMessageType.RedError);
                }
            }
            catch (Exception exc) //Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        protected void UpdateSchedule()
        {
            bool usersOnLineChanged = (Convert.ToBoolean(ViewState["SelectedUsersOnlineEnabled"]) != chkUsersOnline.Checked);
            if (usersOnLineChanged)
            {
                ScheduleItem scheduleItem = SchedulingProvider.Instance().GetSchedule("DotNetNuke.Entities.Users.PurgeUsersOnline, DOTNETNUKE", Null.NullString);
                if (scheduleItem != null)
                {
                    scheduleItem.Enabled = chkUsersOnline.Checked;
                    SchedulingProvider.Instance().UpdateSchedule(scheduleItem);
                }
            }

            bool logBufferChanged = (Convert.ToBoolean(ViewState["SelectedLogBufferEnabled"]) != chkLogBuffer.Checked);
            if (logBufferChanged)
            {
                var scheduleItem = SchedulingProvider.Instance().GetSchedule("DotNetNuke.Services.Log.EventLog.PurgeLogBuffer, DOTNETNUKE", Null.NullString);
                if (scheduleItem != null)
                {
                    scheduleItem.Enabled = chkLogBuffer.Checked;
                    SchedulingProvider.Instance().UpdateSchedule(scheduleItem);
                }
            }
        }

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// cmdUpdate_Click runs when the Upgrade button is clicked
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <history>
        /// 	[cnurse]	9/27/2004	Updated to reflect design changes for Help, 508 support
        ///                       and localisation
        /// </history>
        /// -----------------------------------------------------------------------------
        protected void UpdateSettings(object sender, EventArgs e)
        {
            if (Page.IsValid)
            {
                try
                {
                    // TODO: Remove after refactor: this code/functionality has been copied to ..\AdvancedSettings\SmtpServerSettings.aspx) 
                    //show warning message when set custom smtp port and app running under medium trust, but still can
                    //save the settings because maybe some host providers use a modified medium trusy config and allow
                    //this permission.
                    var smtpServer = txtSMTPServer.Text;
                    var smtpWarning = !string.IsNullOrEmpty(smtpServer)
                                        && smtpServer != DotNetNuke.Entities.Host.Host.SMTPServer
                                        && smtpServer.Contains(":") 
                                        && smtpServer.Split(':')[1] != "25" 
                                        && !SecurityPolicy.HasAspNetHostingPermission();

                    HostController.Instance.Update("CheckUpgrade", chkUpgrade.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("DisplayBetaNotice", chkBetaNotice.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("HostPortalId", hostPortalsCombo.SelectedValue);
                    HostController.Instance.Update("HostTitle", txtHostTitle.Text, false);
                    HostController.Instance.Update("HostURL", txtHostURL.Text, false);
                    HostController.Instance.Update("HostEmail", txtHostEmail.Text.Trim(), false);
                    HostController.Instance.Update("PaymentProcessor", processorCombo.SelectedItem.Text, false);
                    HostController.Instance.Update("ProcessorUserId", txtUserId.Text, false);
                    HostController.Instance.Update("ProcessorPassword", txtPassword.Text, false);
                    HostController.Instance.Update("HostFee", txtHostFee.Text, false);
                    HostController.Instance.Update("HostCurrency", currencyCombo.SelectedValue, false);
                    HostController.Instance.Update("HostSpace", txtHostSpace.Text, false);
                    HostController.Instance.Update("PageQuota", txtPageQuota.Text, false);
                    HostController.Instance.Update("UserQuota", txtUserQuota.Text, false);
                    HostController.Instance.Update("DemoPeriod", txtDemoPeriod.Text, false);
                    HostController.Instance.Update("DemoSignup", chkDemoSignup.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("Copyright", chkCopyright.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("DefaultDocType", docTypeCombo.SelectedValue, false);
                    HostController.Instance.Update("RememberCheckbox", chkRemember.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("EnableCustomModuleCssClass", chkUseCustomModuleCssClass.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("DisableUsersOnline", chkUsersOnline.Checked ? "N" : "Y", false);
                    HostController.Instance.Update("AutoAccountUnlockDuration", txtAutoAccountUnlock.Text, false);
                    HostController.Instance.Update("UsersOnlineTime", txtUsersOnlineTime.Text, false);
                    HostController.Instance.Update("ProxyServer", txtProxyServer.Text, false);
                    HostController.Instance.Update("ProxyPort", txtProxyPort.Text, false);
                    HostController.Instance.Update("ProxyUsername", txtProxyUsername.Text, false);
                    HostController.Instance.Update("ProxyPassword", txtProxyPassword.Text, false);
                    HostController.Instance.Update("WebRequestTimeout", txtWebRequestTimeout.Text, false);
                    // TODO: Refactor: call smtpServerSettings.Update(); This code/functionality has been copied to ..\AdvancedSettings\SmtpServerSettings.aspx) 
                    HostController.Instance.Update("SMTPServer", txtSMTPServer.Text, false);
                    HostController.Instance.Update("SMTPConnectionLimit", txtConnectionLimit.Text, false);
                    HostController.Instance.Update("SMTPMaxIdleTime", txtMaxIdleTime.Text, false);
                    HostController.Instance.Update("SMTPAuthentication", optSMTPAuthentication.SelectedItem.Value, false);
                    HostController.Instance.Update("SMTPUsername", txtSMTPUsername.Text, false);
                    HostController.Instance.UpdateEncryptedString("SMTPPassword", txtSMTPPassword.Text, Config.GetDecryptionkey());
                    HostController.Instance.Update("SMTPEnableSSL", chkSMTPEnableSSL.Checked ? "Y" : "N", false);
                    // end of code copied to smtpServerSettings.Update()
                    HostController.Instance.Update("FileExtensions", txtFileExtensions.Text, false);
                    HostController.Instance.Update("UseCustomErrorMessages", chkUseCustomErrorMessages.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("EnableRequestFilters", chkEnableRequestFilters.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("ControlPanel", cboControlPanel.SelectedItem.Value, false);
                    HostController.Instance.Update("PerformanceSetting", cboPerformance.SelectedItem.Value, false);
                    Entities.Host.Host.PerformanceSetting = (Globals.PerformanceSettings)Enum.Parse(typeof(Globals.PerformanceSettings), cboPerformance.SelectedItem.Value);
                    HostController.Instance.Update("AuthenticatedCacheability", cboCacheability.SelectedItem.Value, false);
                    HostController.Instance.Update("UnauthenticatedCacheability", cboUnauthCacheability.SelectedItem.Value, false);
                    HostController.Instance.Update("PageStatePersister", cboPageState.SelectedItem.Value); 
                    HostController.Instance.Update("ModuleCaching", cboModuleCacheProvider.SelectedItem.Value, false);
                    if (PageCacheRow.Visible)
                    {
                        HostController.Instance.Update("PageCaching", cboPageCacheProvider.SelectedItem.Value, false);
                    }
                    HostController.Instance.Update("EnableModuleOnLineHelp", chkEnableHelp.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("EnableFileAutoSync", chkAutoSync.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("HelpURL", txtHelpURL.Text, false);
                    HostController.Instance.Update("EnableContentLocalization", chkEnableContentLocalization.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("DebugMode", chkDebugMode.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("ShowCriticalErrors", chkCriticalErrors.Checked ? "Y" : "N", true);
                    HostController.Instance.Update("MessageSchedulerBatchSize", txtBatch.Text, false);
                    HostController.Instance.Update("UpgradeForceSSL", chkUpgradeForceSSL.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("SSLDomain", txtSSLDomain.Text, false);
                    
                    HostController.Instance.Update("EventLogBuffer", chkLogBuffer.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("DefaultPortalSkin", hostSkinCombo.SelectedValue, false);
                    HostController.Instance.Update("DefaultAdminSkin", editSkinCombo.SelectedValue, false);
                    HostController.Instance.Update("DefaultPortalContainer", hostContainerCombo.SelectedValue, false);
                    HostController.Instance.Update("DefaultAdminContainer", editContainerCombo.SelectedValue, false);
                    
					HostController.Instance.Update("EnableMsAjaxCDN", chkMsAjaxCdn.Checked ? "Y" : "N", false);
					HostController.Instance.Update("EnableTelerikCDN", chkTelerikCdn.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("CDNEnabled", chkEnableCDN.Checked ? "Y" : "N", false);
					HostController.Instance.Update("TelerikCDNBasicUrl", txtTelerikBasicUrl.Text, false);
					HostController.Instance.Update("TelerikCDNSecureUrl", txtTelerikSecureUrl.Text, false);
                    var maxUpload = 12;
                    if (int.TryParse(txtMaxUploadSize.Text, out maxUpload))
                    {
                        var maxCurrentRequest = Config.GetMaxUploadSize();
                        var maxUploadByMb = (maxUpload*1024*1024);
                        if (maxCurrentRequest != maxUploadByMb)
                        {
                            Config.SetMaxUploadSize(maxUpload * 1024 * 1024);  
                        }
                    };
					HostController.Instance.Update("AsyncTimeout", txtAsyncTimeout.Text, false);
                    HostController.Instance.Update(ClientResourceSettings.EnableCompositeFilesKey, chkCrmEnableCompositeFiles.Checked.ToString(CultureInfo.InvariantCulture));
                    HostController.Instance.Update(ClientResourceSettings.MinifyCssKey, chkCrmMinifyCss.Checked.ToString(CultureInfo.InvariantCulture));
                    HostController.Instance.Update(ClientResourceSettings.MinifyJsKey, chkCrmMinifyJs.Checked.ToString(CultureInfo.InvariantCulture));

                    HostController.Instance.Update("EnableBannedList", chkBannedList.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("EnableStrengthMeter", chkStrengthMeter.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("EnableIPChecking", chkIPChecking.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("EnablePasswordHistory", chkEnablePasswordHistory.Checked ? "Y" : "N", false);
                    HostController.Instance.Update("MembershipResetLinkValidity", txtResetLinkValidity.Text, false);
                    HostController.Instance.Update("AdminMembershipResetLinkValidity", txtAdminResetLinkValidity.Text, false);
                    HostController.Instance.Update("MembershipNumberPasswords", txtNumberPasswords.Text, false);

                    FriendlyUrlsExtensionControl.SaveAction(-1, -1, -1);
                    UpdateSchedule();
                    UpdateSearchIndexConfiguration();
                    UpdateOAuth();

                    // TODO: Remove after refactor: this code/functionality has been copied to ..\AdvancedSettings\SmtpServerSettings.aspx) 
                    var redirectUrl = Request.RawUrl;
                    if (smtpWarning && redirectUrl.IndexOf("smtpwarning=true", StringComparison.InvariantCultureIgnoreCase) == -1)
                    {
                        redirectUrl = string.Format("{0}{1}smtpwarning=true", redirectUrl, redirectUrl.Contains("?") ? "&" : "?");
                    }
                    Response.Redirect(redirectUrl, true);
                }
                catch (Exception exc)
                {
                    Exceptions.ProcessModuleLoadException(this, exc);
                }
                finally
                {
                    //TODO: this is temporary until the AUM Caching is moved into the core.                    
                    DataCache.ClearCache();
                }
            }
        }

        private void UpdateOAuth()
        {
          HostController.Instance.Update("EnableOAuthAuthorization", chkEnableOAuth.Checked ? "Y" : "N", false);
            var settings = OAUTHDataController.GetSettings();
            if (settings==null)
            {
                OAUTHDataController.InsertSettings("PFJTQUtleVZhbHVlPjxNb2R1bHVzPjZlZnpYTHNvSmQ5OVVQMjdOQ1hWSnpZSFVtMmlLTUVOSlo2eVpVSFNvTE5uYnFMdENZWjZXaEl3SXY5WVBGeit0Z1AvQWN0dXh3N2VWVzdsU0RaU0IvQU8ySk1kOXJ1MkJ4SHRVaUZVd2pySnNHTlRUS1NhVDFDOTA0YXFaTExaRUxmYnVvVllabnFKWkRvdjlreTkwaHZtTTYwY3FXbkU4TGc2aGZSYlFPOD08L01vZHVsdXM+PEV4cG9uZW50PkFRQUI8L0V4cG9uZW50PjxQPjlrUjZaNGtabGVMeTlVMlo4WFl3WXd5VTA2d1g2UTR0QytlUitRZm5jOWJJK1loVFFRQXA1RXM1MGZwcWorWE8yQUhnb3RwN1NMRHdLSmZ5UmUzcElRPT08L1A+PFE+OHlacEFEUndhTG45MHFIY1liVG14N094b20wQVlrNVJnaDRyTTF1cGpNTEVHZTF5M01Xa2R0eUs0L3RsamVOeEJmOS82a0w4WTEyRU4zZDluaCtZRHc9PTwvUT48RFA+SW94VXg3V2FHMGJ0WXJCeVNrZVVYakRqcUJaYWZGMTZ3RkxLVGE5b0x2NFF6OERxUFJJeXprNG5YR2ZQRDBUa0ViV2h0L2NDbUo0Sjh3ZnQreFYzUVE9PTwvRFA+PERRPlhpOWd1TzJYSlZuMlVpTFVwUnhYMEw5d2JZUmZnN1RtcWNwWjNaa0NBajVuaTh1SWxMQVNWdUJ6QklDYkErMDRHS1N4dmVLWXRUekNQR0lTeks5Y1RRPT08L0RRPjxJbnZlcnNlUT5kcmVJNmhLaXFOVjZQWXEva2pybENpQk5XaGV1SVRUcGZTS0lEa0ZYd0xLaThrQ2hKcW1FWVltVjFqb2hOOVJEMzNEU2xGcnY0TXZ1cWFUWG5iMXBwdz09PC9JbnZlcnNlUT48RD5uTGZDUUpGTk53TGtyYzB6RHArQ2owRU42dFoxM2FSck1KZUJvNEpVbzBOUXU3b0I0MjNzc0VpYlkvZDlvUVFWek5Ja200azM4YnN1a0VNNjhBVWxNOUJqTGpNZjZmdFF3YlJWbUY0cllPZmZ4czhoUGszRXN3aWoycVlsNmxUMVpUaFM3MHd0MWlyQWQrWmFKNWN5V29HSnZVTWs4cWpQaWNSeEtkbEdub0U9PC9EPjwvUlNBS2V5VmFsdWU+", "PFJTQUtleVZhbHVlPjxNb2R1bHVzPjkwNU9zRjVnYXNIOUVFY0VYV2RaSXNpNlozbWxKRjhlMFlPancrVmY0M0lYTnhmc3ZzOUxvdTR6dVpUOHV5dndpT25jaDUrSXBIOHZTZ2ZzaUZLbFZuQXRzcXhUcU5HVXFBWk5HWG9rZ3FiS0d6WTFoajZLVWxHUlErcThJMHdFbzBrWFh3cjQ3bWFIN01pRVYvaXBiSjZvVmtkbC9XVHJybXMyb2JFR09CRT08L01vZHVsdXM+PEV4cG9uZW50PkFRQUI8L0V4cG9uZW50PjwvUlNBS2V5VmFsdWU+", "PFJTQUtleVZhbHVlPjxNb2R1bHVzPjkwNU9zRjVnYXNIOUVFY0VYV2RaSXNpNlozbWxKRjhlMFlPancrVmY0M0lYTnhmc3ZzOUxvdTR6dVpUOHV5dndpT25jaDUrSXBIOHZTZ2ZzaUZLbFZuQXRzcXhUcU5HVXFBWk5HWG9rZ3FiS0d6WTFoajZLVWxHUlErcThJMHdFbzBrWFh3cjQ3bWFIN01pRVYvaXBiSjZvVmtkbC9XVHJybXMyb2JFR09CRT08L01vZHVsdXM+PEV4cG9uZW50PkFRQUI8L0V4cG9uZW50PjxQPi9DSTYxZ1ZhZzlUMTRkMmdLb0hJSUc2NU5rQ0FzQlVzTlEzMGtRK2l2UEFIWTV5b2JpSXdxTDVxSk54cjhsVUhGMDJxQVR2TUxOYnNaT3J2a3V1bjF3PT08L1A+PFE+K3hrZ0pSQXJhRFRiem9VeElEbVZ4UVZtUVhia1NFS244bnpTYkZEN2ptSzRKd3h5NmlNR2ZTakljdFliNmhxQTc3dFlrTzFpSHVHTnZtS0FHMU9DVnc9PTwvUT48RFA+R1MwUjB1MFY3TFFIR1ZhWDk2YWQ1UjhwUDFHUmlBT1ZObmIrUkwzYThpTEZtaHk2ZE1UVk53Uk1kUUhOaFpVWDhDdkJIZjVxbE0raEt6S0tXWkZPWVE9PTwvRFA+PERRPjRQMnpldUpSTXE5aVlWdWhHREhoREVmNVJ5RmtEWWVFZTFmektGRXNCbnBZYmN6T3p4TVJSbWFicmFKQ0l2TWFvelNvZUR2c1ZxVmVYOEJjNzU5VlF3PT08L0RRPjxJbnZlcnNlUT5vVm5hNG1HQkx5SzN3OHdOQzhGVVBlVHlISzN5SkFSTXdDU0ZTLytTajI5eUdEbCtPeE5CRlNvUW9uWmwwdWFFeFdBN0VJTjJVZUxSZzhicWFELzUyQT09PC9JbnZlcnNlUT48RD5ZTkJQRGN4a2dtYWU0eGhxSlFhb1ptMmVTNVBiaW5tU1h3TGh3WGF5S3lBbTVuSi9ROU56RUwyZmtpODVJU3o2WlI3b0xrL045bGV6ODQ5V2thZUpBYUMzZm96c2Zrek9KVXBOQlNWS1RCRkR6K0dyRHV1a0tLL2JDbDBCVHZnT3E5R0k2UWUwUXpFUnV0SVIwUjY3cXptUUxmenRhVVc4UGVOSTcwTVhHZUU9PC9EPjwvUlNBS2V5VmFsdWU+");
            }
        }

        #endregion

        protected void CompactSearchIndex(object sender, EventArgs e)
        {
            SearchHelper.Instance.SetSearchReindexRequestTime(true);
        }

        protected void HostSearchReindex(object sender, EventArgs e)
        {
            SearchHelper.Instance.SetSearchReindexRequestTime(-1);
        }

        protected void GetSearchIndexStatistics(object sender, EventArgs e)
        {
            var searchStatistics = InternalSearchController.Instance.GetSearchStatistics();
            pnlSearchGetMoreButton.Visible = false;
            pnlSearchStatistics.Visible = true;
            lblSearchIndexDbSize.Text = ((searchStatistics.IndexDbSize/1024f)/1024f).ToString("N") + " MB";
            lblSearchIndexLastModifedOn.Text = DateUtils.CalculateDateForDisplay(searchStatistics.LastModifiedOn);
            lblSearchIndexTotalActiveDocuments.Text = searchStatistics.TotalActiveDocuments.ToString(CultureInfo.InvariantCulture);
            lblSearchIndexTotalDeletedDocuments.Text = searchStatistics.TotalDeletedDocuments.ToString(CultureInfo.InvariantCulture);
        }

        protected void UpdateSearchIndexConfiguration()
        {
            int newMinLength;
            if (int.TryParse(txtIndexWordMinLength.Text, out newMinLength))
            {
                var oldMinLength = HostController.Instance.GetInteger("Search_MinKeyWordLength", 3);
                if (newMinLength != oldMinLength)
                {
                    HostController.Instance.Update("Search_MinKeyWordLength", txtIndexWordMinLength.Text);
                }
            }

            int newMaxLength;
            if (int.TryParse(txtIndexWordMaxLength.Text, out newMaxLength))
            {
                var oldMaxLength = HostController.Instance.GetInteger("Search_MaxKeyWordLength", 255);
                if (newMaxLength != oldMaxLength)
                {
                    HostController.Instance.Update("Search_MaxKeyWordLength", txtIndexWordMaxLength.Text);
                }
            }

			HostController.Instance.Update("Search_AllowLeadingWildcard", chkAllowLeadingWildcard.Checked ? "Y" : "N");

			HostController.Instance.Update(SearchTitleBoostSetting, string.IsNullOrEmpty(txtTitleBoost.Text) ? DefaultSearchTitleBoost.ToString() : txtTitleBoost.Text);
			HostController.Instance.Update(SearchTagBoostSetting, string.IsNullOrEmpty(txtTagBoost.Text) ? DefaultSearchTagBoost.ToString() : txtTagBoost.Text);
			HostController.Instance.Update(SearchContentBoostSetting, string.IsNullOrEmpty(txtContentBoost.Text) ? DefaultSearchContentBoost.ToString() : txtContentBoost.Text);
			HostController.Instance.Update(SearchDescriptionBoostSetting, string.IsNullOrEmpty(txtDescriptionBoost.Text) ? DefaultSearchDescriptionBoost.ToString() : txtDescriptionBoost.Text);
			HostController.Instance.Update(SearchAuthorBoostSetting, string.IsNullOrEmpty(txtAuthorBoost.Text) ? DefaultSearchAuthorBoost.ToString() : txtAuthorBoost.Text);

            var oldAnalyzer = HostController.Instance.GetString("Search_CustomAnalyzer", string.Empty);
            var newAnalyzer = cbCustomAnalyzer.SelectedValue.Trim();
            if (!oldAnalyzer.Equals(newAnalyzer))
            {
                HostController.Instance.Update("Search_CustomAnalyzer", newAnalyzer);
                
                //force the app restart to use new analyzer.
                Config.Touch();
            }
        }
    }
}
