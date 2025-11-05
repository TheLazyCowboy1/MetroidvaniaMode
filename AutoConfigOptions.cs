using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MetroidvaniaMode;

public abstract class AutoConfigOptions : OptionInterface
{
    public class TabAtt : Attribute
    {
        public string Tab, Label, Desc;
        public TabAtt(string tab, string label = "", string desc = "") : base()
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
        public ConfigInfo(ConfigurableBase config, string tab, string label, string desc)
        {
            this.config = config;
            this.tab = tab;
            this.label = label;
            this.desc = desc;
        }
    }

    public AutoConfigOptions(string[] tabs)
    {
        TabNames = tabs;

        //Warp = this.config.Bind<float>("Warp", 25, new ConfigAcceptableRange<float>(-500, 500));
        List<ConfigInfo> configs = new();

        FieldInfo[] fields = GetType().GetFields();
        foreach (FieldInfo info in fields)
        {
            try
            {
                TabAtt att = info.GetCustomAttribute<TabAtt>();
                if (att != null)
                {
                    ConfigurableBase configBase = (ConfigurableBase)typeof(ConfigHolder).GetMethods().First(m => m.Name == nameof(ConfigHolder.Bind)).MakeGenericMethod(info.FieldType)
                        .Invoke(config, new object[] { info.Name, info.GetValue(this), null });

                    LimitRange rangeAtt = info.GetCustomAttribute<LimitRange>();
                    if (rangeAtt != null)
                    {
                        configBase.info.acceptable = (ConfigAcceptableBase)Activator.CreateInstance(typeof(ConfigAcceptableRange<>).MakeGenericType(info.FieldType), rangeAtt.Min, rangeAtt.Max);
                    }

                    configs.Add(new(configBase, att.Tab, att.Label, att.Desc));
                }
            } catch (Exception ex) { Plugin.Error(ex); }
        }

        ConfigInfos = configs.ToArray();
        Plugin.Log("Found " + ConfigInfos.Length + " configs");
    }

    //General
    //public readonly Configurable<float> Warp;

    private ConfigInfo[] ConfigInfos;
    private string[] TabNames;

    public override void Initialize()
    {
        this.Tabs = new OpTab[TabNames.Length];
        for (int i = 0; i < TabNames.Length; i++)
        {
            this.Tabs[i] = new(this, TabNames[i]);

            float t = 150f, y = 550f, h = -40f, H = -70f, x = 50f, w = 80f, c = 50f;
            //float t2 = 400f, x2 = 300f;

            foreach (ConfigInfo info in ConfigInfos)
            {
                if (info.tab == TabNames[i])
                {
                    UIelement el;
                    if (info.config is Configurable<bool> cb)
                        el = new OpCheckBox(cb, x + c, y) { description = info.desc };
                    else if (info.config is Configurable<float> cf)
                        el = new OpUpdown(cf, new(x, y), w) { description = info.desc };
                    else if (info.config is Configurable<int> ci)
                        el = new OpUpdown(ci, new(x, y), w) { description = info.desc };
                    else
                    {
                        Plugin.Error("This config type is not yet supported: " + info.config.GetType().FullName);
                        continue;
                    }

                    this.Tabs[i].AddItems(new OpLabel(t, y, info.label), el);
                    y += h;
                }
            }
        }

        /*var optionsTab = new OpTab(this, "Options");
        this.Tabs = new[]
        {
            optionsTab
        };

        OpHoldButton clearAchievementButton;

        float t = 150f, y = 550f, h = -40f, H = -70f, x = 50f, w = 80f, c = 50f;
        float t2 = 400f, x2 = 300f;

        optionsTab.AddItems(

            clearAchievementButton = new OpHoldButton(new(50, 50), new Vector2(150, 40), "Clear Achievements") { description = "Clears all achievements for the currently active save file." }
            );

        clearAchievementButton.OnPressDone += (trigger) =>
        {
            //AchievementManager.ClearAllAchievements();
            trigger?.Menu?.PlaySound(SoundID.MENU_Checkbox_Check);
        };*/
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