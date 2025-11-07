using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MetroidvaniaMode;

public abstract class AutoConfigOptions : OptionInterface
{
    public class Config : Attribute
    {
        public string Tab, Label, Desc;
        public bool rightSide = false;
        public bool hide = false;
        public float width = -1f, spaceBefore = 0f, spaceAfter = 0f, height = -1f;
        /// <summary>
        /// Used for float configs
        /// </summary>
        public byte precision = 2;
        /// <summary>
        /// Used for string configs. Makes the config a dropdown choice-selection box instead of a textbox.
        /// </summary>
        public string[] dropdownOptions = null;
        public Config(string tab, string label, string desc) : base()
        {
            this.Tab = tab;
            this.Label = label;
            this.Desc = desc;
        }
    }

    public class LimitRange : Attribute
    {
        public IComparable Min, Max;
        public LimitRange(float min, float max) : base()
        {
            this.Min = min;
            this.Max = max;
        }
    }

    private struct ConfigInfo
    {
        public ConfigurableBase config;
        public string tab;
        public string label;
        public string desc;
        public bool rightSide;
        public bool hide;
        public float width, spaceBefore, spaceAfter, height;
        public byte precision;
        public string[] dropdownOptions;
    }

    public struct TabInfo
    {
        public string name;
        public float startHeight = 550f, spacing = 40f, leftMargin = 50f,
            textOffset = 90f, updownWidth = 80f, checkboxOffset = 55f,
            rightMargin = 300f, defaultHeight = 25f;
        public TabInfo(string name)
        {
            this.name = name;
        }
    }

    public AutoConfigOptions(TabInfo[] tabs)
    {
        TabInfos = tabs;

        List<ConfigInfo> configs = new();

        FieldInfo[] fields = GetType().GetFields();
        foreach (FieldInfo info in fields)
        {
            try
            {
                Config att = info.GetCustomAttribute<Config>();
                if (att != null)
                {
                    ConfigurableBase configBase = (ConfigurableBase)typeof(ConfigHolder).GetMethods().First(m => m.Name == nameof(ConfigHolder.Bind)).MakeGenericMethod(info.FieldType)
                        .Invoke(config, new object[] { info.Name, info.GetValue(this), null });

                    LimitRange rangeAtt = info.GetCustomAttribute<LimitRange>();
                    if (rangeAtt != null)
                    {
                        configBase.info.acceptable = (ConfigAcceptableBase)Activator.CreateInstance(typeof(ConfigAcceptableRange<>).MakeGenericType(info.FieldType), rangeAtt.Min, rangeAtt.Max);
                    }

                    configs.Add(new() { config = configBase, tab = att.Tab, label = att.Label, desc = att.Desc,
                        hide = att.hide, rightSide = att.rightSide, width = att.width, spaceBefore = att.spaceBefore,
                        spaceAfter = att.spaceAfter, height = att.height, precision = att.precision,
                        dropdownOptions = att.dropdownOptions
                    });
                }
            } catch (Exception ex) { Plugin.Error(ex); }
        }

        ConfigInfos = configs.ToArray();
        Plugin.Log("Found " + ConfigInfos.Length + " configs");
    }

    private ConfigInfo[] ConfigInfos;
    public TabInfo[] TabInfos;

    public override void Initialize()
    {
        this.Tabs = new OpTab[TabInfos.Length];
        for (int i = 0; i < TabInfos.Length; i++)
        {
            TabInfo tInfo = TabInfos[i];
            string name = tInfo.name;
            this.Tabs[i] = new(this, name);

            float y = tInfo.startHeight;

            foreach (ConfigInfo cInfo in ConfigInfos)
            {
                if (cInfo.tab == name)
                {
                    float x = cInfo.rightSide ? tInfo.rightMargin : tInfo.leftMargin;
                    float w = cInfo.width >= 0 ? cInfo.width : tInfo.updownWidth;
                    float h = cInfo.height >= 0 ? cInfo.height : tInfo.defaultHeight;
                    float t = tInfo.textOffset + w - tInfo.updownWidth; //updownWidth is the default

                    if (cInfo.rightSide)
                        y += tInfo.spacing; //keep on same height... a janky method to do so, but oh well
                    y -= cInfo.spaceBefore;

                    UIelement el;
                    if (cInfo.config is Configurable<bool> cb)
                        el = new OpCheckBox(cb, x + tInfo.checkboxOffset, y);
                    else if (cInfo.config is Configurable<float> cf)
                        el = new OpUpdown(cf, new(x, y), w, cInfo.precision);
                    else if (cInfo.config is Configurable<int> ci)
                        el = new OpUpdown(ci, new(x, y), w);
                    else if (cInfo.config is Configurable<KeyCode> ck)
                        el = new OpKeyBinder(ck, new(x, y), new(w, h));
                    else if (cInfo.config is Configurable<string> cs)
                    {
                        if (cInfo.dropdownOptions != null)
                            el = new OpComboBox(cs, new(x, y), w, cInfo.dropdownOptions);
                        else
                            el = new OpTextBox(cs, new(x, y), w);
                    }
                    else
                    {
                        Plugin.Error("This config type is not yet supported: " + cInfo.config.GetType().FullName);
                        continue;
                    }
                    el.description = cInfo.desc;

                    this.Tabs[i].AddItems(new OpLabel(x + t, y, cInfo.label), el);
                    y -= tInfo.spacing + cInfo.spaceAfter;
                }
            }
        }

        MenuInitialized();
    }

    /// <summary>
    /// Use to add any additional elements, such as buttons
    /// </summary>
    public virtual void MenuInitialized()
    {

    }


    public void SetValues()
    {
        Type type = this.GetType();
        foreach (ConfigInfo info in ConfigInfos)
        {
            try
            {
                type.GetField(info.config.key).SetValue(this, info.config.BoxedValue);
            } catch (Exception ex) { Plugin.Error(ex); }
        }
        Plugin.Log("Set config values");
    }

}