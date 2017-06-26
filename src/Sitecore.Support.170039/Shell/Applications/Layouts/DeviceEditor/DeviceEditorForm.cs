namespace Sitecore.Support.Shell.Applications.Layouts.DeviceEditor
{
    using Data;
    using Globalization;
    using Resources;
    using Rules;
    using SecurityModel;
    using Sitecore;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Layouts;
    using Sitecore.Pipelines.RenderDeviceEditorRendering;
    using Sitecore.Shell.Applications.Dialogs;
    using Sitecore.Shell.Applications.Dialogs.ItemLister;
    using Sitecore.Shell.Applications.Dialogs.Personalize;
    using Sitecore.Shell.Applications.Dialogs.Testing;
    using Sitecore.Shell.Applications.Layouts.DeviceEditor;
    using Sitecore.Web;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Pages;
    using Sitecore.Web.UI.Sheer;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.UI.HtmlControls;
    using System.Xml.Linq;
    using Web.UI.XmlControls;
    using Xdb.Configuration;

    [UsedImplicitly]
    public class DeviceEditorForm : DialogForm
    {
        // Methods
        [HandleMessage("device:add", true), UsedImplicitly]
        protected void Add(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                if (args.HasResult)
                {
                    string[] strArray = args.Result.Split(new char[] { ',' });
                    string str = strArray[0];
                    string str2 = strArray[1].Replace("-c-", ",");
                    bool flag = strArray[2] == "1";
                    LayoutDefinition layoutDefinition = GetLayoutDefinition();
                    DeviceDefinition device = layoutDefinition.GetDevice(this.DeviceID);
                    RenderingDefinition renderingDefinition = new RenderingDefinition
                    {
                        ItemID = str,
                        Placeholder = str2
                    };
                    device.AddRendering(renderingDefinition);
                    SetDefinition(layoutDefinition);
                    this.Refresh();
                    if (flag)
                    {
                        ArrayList renderings = device.Renderings;
                        if (renderings != null)
                        {
                            this.SelectedIndex = renderings.Count - 1;
                            Context.ClientPage.SendMessage(this, "device:edit");
                        }
                    }
                    Registry.SetString("/Current_User/SelectRendering/Selected", str);
                }
            }
            else
            {
                SelectRenderingOptions options = new SelectRenderingOptions
                {
                    ShowOpenProperties = true,
                    ShowPlaceholderName = true,
                    PlaceholderName = string.Empty
                };
                string str3 = Registry.GetString("/Current_User/SelectRendering/Selected");
                if (!string.IsNullOrEmpty(str3))
                {
                    options.SelectedItem = Client.ContentDatabase.GetItem(str3);
                }
                SheerResponse.ShowModalDialog(options.ToUrlString(Client.ContentDatabase).ToString(), true);
                args.WaitForPostBack();
            }
        }

        [UsedImplicitly, HandleMessage("device:addplaceholder", true)]
        protected void AddPlaceholder(ClientPipelineArgs args)
        {
            if (args.IsPostBack)
            {
                if (!string.IsNullOrEmpty(args.Result) && (args.Result != "undefined"))
                {
                    string str;
                    LayoutDefinition layoutDefinition = GetLayoutDefinition();
                    DeviceDefinition device = layoutDefinition.GetDevice(this.DeviceID);
                    Item item = SelectPlaceholderSettingsOptions.ParseDialogResult(args.Result, Client.ContentDatabase, out str);
                    if ((item != null) && !string.IsNullOrEmpty(str))
                    {
                        PlaceholderDefinition placeholderDefinition = new PlaceholderDefinition
                        {
                            UniqueId = ID.NewID.ToString(),
                            MetaDataItemId = item.Paths.FullPath,
                            Key = str
                        };
                        device.AddPlaceholder(placeholderDefinition);
                        SetDefinition(layoutDefinition);
                        this.Refresh();
                    }
                }
            }
            else
            {
                SelectPlaceholderSettingsOptions options = new SelectPlaceholderSettingsOptions
                {
                    IsPlaceholderKeyEditable = true
                };
                SheerResponse.ShowModalDialog(options.ToUrlString().ToString(), "460px", "460px", string.Empty, true);
                args.WaitForPostBack();
            }
        }

        [HandleMessage("device:change", true), UsedImplicitly]
        protected void Change(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (this.SelectedIndex >= 0)
            {
                LayoutDefinition layoutDefinition = GetLayoutDefinition();
                ArrayList renderings = layoutDefinition.GetDevice(this.DeviceID).Renderings;
                if (renderings != null)
                {
                    RenderingDefinition definition3 = renderings[this.SelectedIndex] as RenderingDefinition;
                    if ((definition3 != null) && !string.IsNullOrEmpty(definition3.ItemID))
                    {
                        if (args.IsPostBack)
                        {
                            if (args.HasResult)
                            {
                                string[] strArray = args.Result.Split(new char[] { ',' });
                                definition3.ItemID = strArray[0];
                                bool flag = strArray[2] == "1";
                                SetDefinition(layoutDefinition);
                                this.Refresh();
                                if (flag)
                                {
                                    Context.ClientPage.SendMessage(this, "device:edit");
                                }
                            }
                        }
                        else
                        {
                            SelectRenderingOptions options = new SelectRenderingOptions
                            {
                                ShowOpenProperties = true,
                                ShowPlaceholderName = false,
                                PlaceholderName = string.Empty,
                                SelectedItem = Client.ContentDatabase.GetItem(definition3.ItemID)
                            };
                            SheerResponse.ShowModalDialog(options.ToUrlString(Client.ContentDatabase).ToString(), true);
                            args.WaitForPostBack();
                        }
                    }
                }
            }
        }

        private void ChangeButtonsState(bool disable)
        {
            this.Personalize.Disabled = disable;
            this.btnEdit.Disabled = disable;
            this.btnChange.Disabled = disable;
            this.btnRemove.Disabled = disable;
            this.MoveUp.Disabled = disable;
            this.MoveDown.Disabled = disable;
            this.Test.Disabled = disable;
        }

        [HandleMessage("device:edit", true), UsedImplicitly]
        protected void Edit(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            RenderingParameters parameters = new RenderingParameters
            {
                Args = args,
                HandleName = GetSessionHandle(),
                DeviceId = this.DeviceID,
                SelectedIndex = this.SelectedIndex,
                Item = UIUtil.GetItemFromQueryString(Client.ContentDatabase)
            };
            if (parameters.Show())
            {
                this.Refresh();
            }
        }

        [UsedImplicitly, HandleMessage("device:editplaceholder", true)]
        protected void EditPlaceholder(ClientPipelineArgs args)
        {
            if (!string.IsNullOrEmpty(this.UniqueId))
            {
                LayoutDefinition layoutDefinition = GetLayoutDefinition();
                PlaceholderDefinition placeholder = layoutDefinition.GetDevice(this.DeviceID).GetPlaceholder(this.UniqueId);
                if (placeholder != null)
                {
                    if (args.IsPostBack)
                    {
                        if (!string.IsNullOrEmpty(args.Result) && (args.Result != "undefined"))
                        {
                            string str;
                            Item item = SelectPlaceholderSettingsOptions.ParseDialogResult(args.Result, Client.ContentDatabase, out str);
                            if (item != null)
                            {
                                placeholder.MetaDataItemId = item.Paths.FullPath;
                                placeholder.Key = str;
                                SetDefinition(layoutDefinition);
                                this.Refresh();
                            }
                        }
                    }
                    else
                    {
                        Item item2 = string.IsNullOrEmpty(placeholder.MetaDataItemId) ? null : Client.ContentDatabase.GetItem(placeholder.MetaDataItemId);
                        SelectPlaceholderSettingsOptions options = new SelectPlaceholderSettingsOptions
                        {
                            TemplateForCreating = null,
                            PlaceholderKey = placeholder.Key,
                            CurrentSettingsItem = item2,
                            SelectedItem = item2,
                            IsPlaceholderKeyEditable = true
                        };
                        SheerResponse.ShowModalDialog(options.ToUrlString().ToString(), "460px", "460px", string.Empty, true);
                        args.WaitForPostBack();
                    }
                }
            }
        }

        private static LayoutDefinition GetLayoutDefinition()
        {
            string sessionString = WebUtil.GetSessionString(GetSessionHandle());
            Assert.IsNotNull(sessionString, "layout definition");
            return LayoutDefinition.Parse(sessionString);
        }

        private static string GetSessionHandle() =>
            "SC_DEVICEEDITOR_" + GetCurrentItem().ID.Guid.ToString();

        private static bool HasRenderingRules(RenderingDefinition definition)
        {
            if (definition.Rules != null)
            {
                IEnumerable<XElement> rules = new RulesDefinition(definition.Rules.ToString()).GetRules();
                if (rules == null)
                {
                    return false;
                }
                foreach (XElement element in rules)
                {
                    XElement element2 = element.Descendants("actions").FirstOrDefault<XElement>();
                    if ((element2 != null) && element2.Descendants().Any<XElement>())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                this.DeviceID = WebUtil.GetQueryString("de");
                DeviceDefinition device = GetLayoutDefinition().GetDevice(this.DeviceID);
                if (device.Layout != null)
                {
                    this.Layout.Value = device.Layout;
                }
                this.Personalize.Visible = Policy.IsAllowed("Page Editor/Extended features/Personalization");
                this.Test.Visible = XdbSettings.Enabled && Policy.IsAllowed("Page Editor/Extended features/Testing");
                this.Refresh();
                this.SelectedIndex = -1;
            }
        }

        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            if (this.Layout.Value.Length > 0)
            {
                Item item = Client.ContentDatabase.GetItem(this.Layout.Value);
                if (item == null)
                {
                    Context.ClientPage.ClientResponse.Alert("Layout not found.");
                    return;
                }
                if ((item.TemplateID == TemplateIDs.Folder) || (item.TemplateID == TemplateIDs.Node))
                {
                    Context.ClientPage.ClientResponse.Alert(Translate.Text("\"{0}\" is not a layout.", new object[] { item.DisplayName }));
                    return;
                }
            }
            LayoutDefinition layoutDefinition = GetLayoutDefinition();
            DeviceDefinition device = layoutDefinition.GetDevice(this.DeviceID);
            ArrayList renderings = device.Renderings;
            if (((renderings != null) && (renderings.Count > 0)) && (this.Layout.Value.Length == 0))
            {
                Context.ClientPage.ClientResponse.Alert("You must specify a layout when you specify renderings.");
            }
            else
            {
                device.Layout = this.Layout.Value;
                SetDefinition(layoutDefinition);
                Context.ClientPage.ClientResponse.SetDialogValue("yes");
                base.OnOK(sender, args);
            }
        }

        [UsedImplicitly]
        protected void OnPlaceholderClick(string uniqueId)
        {
            Assert.ArgumentNotNullOrEmpty(uniqueId, "uniqueId");
            if (!string.IsNullOrEmpty(this.UniqueId))
            {
                SheerResponse.SetStyle("ph_" + ID.Parse(this.UniqueId).ToShortID(), "background", string.Empty);
            }
            this.UniqueId = uniqueId;
            if (!string.IsNullOrEmpty(uniqueId))
            {
                SheerResponse.SetStyle("ph_" + ID.Parse(uniqueId).ToShortID(), "background", "#D0EBF6");
            }
            this.UpdatePlaceholdersCommandsState();
        }

        [UsedImplicitly]
        protected void OnRenderingClick(string index)
        {
            Assert.ArgumentNotNull(index, "index");
            if (this.SelectedIndex >= 0)
            {
                SheerResponse.SetStyle(StringUtil.GetString(this.Controls[this.SelectedIndex]), "background", string.Empty);
            }
            this.SelectedIndex = MainUtil.GetInt(index, -1);
            if (this.SelectedIndex >= 0)
            {
                SheerResponse.SetStyle(StringUtil.GetString(this.Controls[this.SelectedIndex]), "background", "#D0EBF6");
            }
            this.UpdateRenderingsCommandsState();
        }

        [UsedImplicitly, HandleMessage("device:personalize", true)]
        protected void PersonalizeControl(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (this.SelectedIndex >= 0)
            {
                LayoutDefinition layoutDefinition = GetLayoutDefinition();
                ArrayList renderings = layoutDefinition.GetDevice(this.DeviceID).Renderings;
                if (renderings != null)
                {
                    RenderingDefinition definition3 = renderings[this.SelectedIndex] as RenderingDefinition;
                    if ((definition3 != null) && (!string.IsNullOrEmpty(definition3.ItemID) && !string.IsNullOrEmpty(definition3.UniqueId)))
                    {
                        if (args.IsPostBack)
                        {
                            if (args.HasResult)
                            {
                                XElement element = XElement.Parse(args.Result);
                                definition3.Rules = element;
                                SetDefinition(layoutDefinition);
                                this.Refresh();
                            }
                        }
                        else
                        {
                            Item itemFromQueryString = UIUtil.GetItemFromQueryString(Client.ContentDatabase);
                            string str = (itemFromQueryString != null) ? itemFromQueryString.Uri.ToString() : string.Empty;
                            PersonalizeOptions options = new PersonalizeOptions
                            {
                                SessionHandle = GetSessionHandle(),
                                DeviceId = this.DeviceID,
                                RenderingUniqueId = definition3.UniqueId,
                                ContextItemUri = str
                            };
                            SheerResponse.ShowModalDialog(options.ToUrlString().ToString(), "980px", "712px", string.Empty, true);
                            args.WaitForPostBack();
                        }
                    }
                }
            }
        }

        private void Refresh()
        {
            this.Renderings.Controls.Clear();
            this.Placeholders.Controls.Clear();
            this.Controls = new ArrayList();
            DeviceDefinition device = GetLayoutDefinition().GetDevice(this.DeviceID);
            if (device.Renderings == null)
            {
                SheerResponse.SetOuterHtml("Renderings", this.Renderings);
                SheerResponse.SetOuterHtml("Placeholders", this.Placeholders);
                SheerResponse.Eval("if (!scForm.browser.isIE) { scForm.browser.initializeFixsizeElements(); }");
            }
            else
            {
                int selectedIndex = this.SelectedIndex;
                this.RenderRenderings(device, selectedIndex, 0);
                this.RenderPlaceholders(device);
                this.UpdateRenderingsCommandsState();
                this.UpdatePlaceholdersCommandsState();
                SheerResponse.SetOuterHtml("Renderings", this.Renderings);
                SheerResponse.SetOuterHtml("Placeholders", this.Placeholders);
                SheerResponse.Eval("if (!scForm.browser.isIE) { scForm.browser.initializeFixsizeElements(); }");
            }
        }

        [UsedImplicitly, HandleMessage("device:remove")]
        protected void Remove(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            int selectedIndex = this.SelectedIndex;
            if (selectedIndex >= 0)
            {
                LayoutDefinition layoutDefinition = GetLayoutDefinition();
                ArrayList renderings = layoutDefinition.GetDevice(this.DeviceID).Renderings;
                if ((renderings != null) && ((selectedIndex >= 0) && (selectedIndex < renderings.Count)))
                {
                    renderings.RemoveAt(selectedIndex);
                    if (selectedIndex >= 0)
                    {
                        this.SelectedIndex--;
                    }
                    SetDefinition(layoutDefinition);
                    this.Refresh();
                }
            }
        }

        [HandleMessage("device:removeplaceholder"), UsedImplicitly]
        protected void RemovePlaceholder(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (!string.IsNullOrEmpty(this.UniqueId))
            {
                LayoutDefinition layoutDefinition = GetLayoutDefinition();
                DeviceDefinition device = layoutDefinition.GetDevice(this.DeviceID);
                PlaceholderDefinition placeholder = device.GetPlaceholder(this.UniqueId);
                if (placeholder != null)
                {
                    ArrayList placeholders = device.Placeholders;
                    if (placeholders != null)
                    {
                        placeholders.Remove(placeholder);
                    }
                    SetDefinition(layoutDefinition);
                    this.Refresh();
                }
            }
        }

        private void RenderPlaceholders(DeviceDefinition deviceDefinition)
        {
            Assert.ArgumentNotNull(deviceDefinition, "deviceDefinition");
            ArrayList placeholders = deviceDefinition.Placeholders;
            if (placeholders != null)
            {
                foreach (PlaceholderDefinition definition in placeholders)
                {
                    Item item = null;
                    string metaDataItemId = definition.MetaDataItemId;
                    if (!string.IsNullOrEmpty(metaDataItemId))
                    {
                        item = Client.ContentDatabase.GetItem(metaDataItemId);
                    }
                    XmlControl webControl = Resource.GetWebControl("DeviceRendering") as XmlControl;
                    Assert.IsNotNull(webControl, typeof(XmlControl));
                    this.Placeholders.Controls.Add(webControl);
                    ID id = ID.Parse(definition.UniqueId);
                    if (definition.UniqueId == this.UniqueId)
                    {
                        webControl["Background"] = "#D0EBF6";
                    }
                    string str2 = "ph_" + id.ToShortID();
                    webControl["ID"] = str2;
                    webControl["Header"] = definition.Key;
                    webControl["Click"] = "OnPlaceholderClick(\"" + definition.UniqueId + "\")";
                    webControl["DblClick"] = "device:editplaceholder";
                    if (item != null)
                    {
                        webControl["Icon"] = item.Appearance.Icon;
                    }
                    else
                    {
                        webControl["Icon"] = "Imaging/24x24/layer_blend.png";
                    }
                }
            }
        }

        private void RenderRenderings(DeviceDefinition deviceDefinition, int selectedIndex, int index)
        {
            Assert.ArgumentNotNull(deviceDefinition, "deviceDefinition");
            ArrayList renderings = deviceDefinition.Renderings;
            if (renderings != null)
            {
                foreach (RenderingDefinition definition in renderings)
                {
                    if (definition.ItemID != null)
                    {
                        Item item = Client.ContentDatabase.GetItem(definition.ItemID);
                        XmlControl webControl = Resource.GetWebControl("DeviceRendering") as XmlControl;
                        Assert.IsNotNull(webControl, typeof(XmlControl));
                        HtmlGenericControl child = new HtmlGenericControl("div");
                        child.Style.Add("padding", "0");
                        child.Style.Add("margin", "0");
                        child.Style.Add("border", "0");
                        child.Style.Add("position", "relative");
                        child.Controls.Add(webControl);
                        string uniqueID = Control.GetUniqueID("R");
                        this.Renderings.Controls.Add(child);
                        child.ID = Control.GetUniqueID("C");
                        webControl["Click"] = "OnRenderingClick(\"" + index + "\")";
                        webControl["DblClick"] = "device:edit";
                        if (index == selectedIndex)
                        {
                            webControl["Background"] = "#D0EBF6";
                        }
                        this.Controls.Add(uniqueID);
                        if (item != null)
                        {
                            webControl["ID"] = uniqueID;
                            webControl["Icon"] = item.Appearance.Icon;
                            webControl["Header"] = item.DisplayName;
                            webControl["Placeholder"] = WebUtil.SafeEncode(definition.Placeholder);
                        }
                        else
                        {
                            webControl["ID"] = uniqueID;
                            webControl["Icon"] = "Applications/24x24/forbidden.png";
                            webControl["Header"] = "Unknown rendering";
                            webControl["Placeholder"] = string.Empty;
                        }
                        if ((definition.Rules != null) && !definition.Rules.IsEmpty)
                        {
                            int num = definition.Rules.Elements("rule").Count<XElement>();
                            if (num > 1)
                            {
                                HtmlGenericControl control3 = new HtmlGenericControl("span");
                                if (num > 9)
                                {
                                    control3.Attributes["class"] = "scConditionContainer scLongConditionContainer";
                                }
                                else
                                {
                                    control3.Attributes["class"] = "scConditionContainer";
                                }
                                control3.InnerText = num.ToString();
                                child.Controls.Add(control3);
                            }
                        }
                        RenderDeviceEditorRenderingPipeline.Run(definition, webControl, child);
                        index++;
                    }
                }
            }
        }

        private static void SetDefinition(LayoutDefinition layout)
        {
            Assert.ArgumentNotNull(layout, "layout");
            string str = layout.ToXml();
            WebUtil.SetSessionValue(GetSessionHandle(), str);
        }

        [HandleMessage("device:test", true), UsedImplicitly]
        protected void SetTest(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (this.SelectedIndex >= 0)
            {
                LayoutDefinition layoutDefinition = GetLayoutDefinition();
                DeviceDefinition device = layoutDefinition.GetDevice(this.DeviceID);
                ArrayList renderings = device.Renderings;
                if (renderings != null)
                {
                    RenderingDefinition definition3 = renderings[this.SelectedIndex] as RenderingDefinition;
                    if (definition3 != null)
                    {
                        if (args.IsPostBack)
                        {
                            if (args.HasResult)
                            {
                                if (args.Result == "#reset#")
                                {
                                    definition3.MultiVariateTest = string.Empty;
                                    SetDefinition(layoutDefinition);
                                    this.Refresh();
                                }
                                else
                                {
                                    ID id = SetTestDetailsOptions.ParseDialogResult(args.Result);
                                    if (ID.IsNullOrEmpty(id))
                                    {
                                        SheerResponse.Alert("Item not found.", new string[0]);
                                    }
                                    else
                                    {
                                        definition3.MultiVariateTest = id.ToString();
                                        SetDefinition(layoutDefinition);
                                        this.Refresh();
                                    }
                                }
                            }
                        }
                        else
                        {
                            Item itemFromQueryString = UIUtil.GetItemFromQueryString(Client.ContentDatabase);
                            SetTestDetailsOptions options = new SetTestDetailsOptions(GetSessionHandle(), itemFromQueryString.Uri.ToString(), device.ID, definition3.UniqueId);
                            SheerResponse.ShowModalDialog(options.ToUrlString().ToString(), "520px", "695px", string.Empty, true);
                            args.WaitForPostBack();
                        }
                    }
                }
            }
        }

        [HandleMessage("device:sortdown"), UsedImplicitly]
        protected void SortDown(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (this.SelectedIndex >= 0)
            {
                LayoutDefinition layoutDefinition = GetLayoutDefinition();
                ArrayList renderings = layoutDefinition.GetDevice(this.DeviceID).Renderings;
                if ((renderings != null) && (this.SelectedIndex < (renderings.Count - 1)))
                {
                    RenderingDefinition definition3 = renderings[this.SelectedIndex] as RenderingDefinition;
                    if (definition3 != null)
                    {
                        renderings.Remove(definition3);
                        renderings.Insert(this.SelectedIndex + 1, definition3);
                        this.SelectedIndex++;
                        SetDefinition(layoutDefinition);
                        this.Refresh();
                    }
                }
            }
        }

        [HandleMessage("device:sortup"), UsedImplicitly]
        protected void SortUp(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            if (this.SelectedIndex > 0)
            {
                LayoutDefinition layoutDefinition = GetLayoutDefinition();
                ArrayList renderings = layoutDefinition.GetDevice(this.DeviceID).Renderings;
                if (renderings != null)
                {
                    RenderingDefinition definition3 = renderings[this.SelectedIndex] as RenderingDefinition;
                    if (definition3 != null)
                    {
                        renderings.Remove(definition3);
                        renderings.Insert(this.SelectedIndex - 1, definition3);
                        this.SelectedIndex--;
                        SetDefinition(layoutDefinition);
                        this.Refresh();
                    }
                }
            }
        }

        private void UpdatePlaceholdersCommandsState()
        {
            this.phEdit.Disabled = string.IsNullOrEmpty(this.UniqueId);
            this.phRemove.Disabled = string.IsNullOrEmpty(this.UniqueId);
        }

        private void UpdateRenderingsCommandsState()
        {
            if (this.SelectedIndex < 0)
            {
                this.ChangeButtonsState(true);
            }
            else
            {
                ArrayList renderings = GetLayoutDefinition().GetDevice(this.DeviceID).Renderings;
                if (renderings == null)
                {
                    this.ChangeButtonsState(true);
                }
                else
                {
                    RenderingDefinition definition = renderings[this.SelectedIndex] as RenderingDefinition;
                    if (definition == null)
                    {
                        this.ChangeButtonsState(true);
                    }
                    else
                    {
                        this.ChangeButtonsState(false);
                        this.Personalize.Disabled = !string.IsNullOrEmpty(definition.MultiVariateTest);
                        this.Test.Disabled = HasRenderingRules(definition);
                    }
                }
            }
        }

        // Properties
        protected Button btnChange { get; set; }

        protected Button btnEdit { get; set; }

        protected Button btnRemove { get; set; }

        public ArrayList Controls
        {
            get
            {
                return
                ((ArrayList)Context.ClientPage.ServerProperties["Controls"]);
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                Context.ClientPage.ServerProperties["Controls"] = value;
            }
        }

        public string DeviceID
        {
            get
            {
                return
                StringUtil.GetString(Context.ClientPage.ServerProperties["DeviceID"]);
            }
            set
            {
                Assert.ArgumentNotNullOrEmpty(value, "value");
                Context.ClientPage.ServerProperties["DeviceID"] = value;
            }
        }

        protected TreePicker Layout { get; set; }

        protected Button MoveDown { get; set; }

        protected Button MoveUp { get; set; }

        protected Button Personalize { get; set; }

        protected Button phEdit { get; set; }

        protected Button phRemove { get; set; }

        protected Scrollbox Placeholders { get; set; }

        protected Scrollbox Renderings { get; set; }

        public int SelectedIndex
        {
            get
            {
                return
                MainUtil.GetInt(Context.ClientPage.ServerProperties["SelectedIndex"], -1);
            }
            set
            {
                Context.ClientPage.ServerProperties["SelectedIndex"] = value;
            }
        }

        protected Button Test { get; set; }

        public string UniqueId
        {
            get
            {
                return
             StringUtil.GetString(Context.ClientPage.ServerProperties["PlaceholderUniqueID"]);
            }
            set
            {
                Assert.ArgumentNotNullOrEmpty(value, "value");
                Context.ClientPage.ServerProperties["PlaceholderUniqueID"] = value;
            }
        }

        private static Item GetCurrentItem()
        {
            string queryString = WebUtil.GetQueryString("id");
            Language language = Language.Parse(WebUtil.GetQueryString("la"));
            Sitecore.Data.Version version = Data.Version.Parse(WebUtil.GetQueryString("vs"));
            return Client.ContentDatabase.GetItem(queryString, language, version);
        }
    }
}

