﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BSI_eBayOrders.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "12.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.ConnectionString)]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=192.168.1.22;Initial Catalog=berkeley;Persist Security Info=True;User" +
            " ID=developer;Password=zaoreo112233")]
        public string berkeleyConnectionString {
            get {
                return ((string)(this["berkeleyConnectionString"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"AgAAAA**AQAAAA**aAAAAA**CWDnTg**nY+sHZ2PrBmdj6wVnY+sEZ2PrA2dj6wDmYehAZeEpQqdj6x9nY+seQ**XVYBAA**AAMAAA**3UP5fYaUEZnVm5ZwIKhfgrd6WlQaLqGz0OMteL6wmx8NC8yeWZ+mjiEGPLF1EmMv3x5vER+4BNc+r7575cEzacBqUQf97x8JkCZnYf0YV6ykxpVYppX+AwjqfdDA3jMvwrag6g8d5p+F8MxW/48iMJ9SgccbU05FngzWC1uOwWxRivSJeMaIcLPhLXrbaWbpEKfbXb2qHRBcUZ5za4XJdFEZaVnaUMidy7qlgLIxuEhkvvrn8NfJcklaE9vgAEK7CEGX4W86nxTKa+HFykcICTocrDBvVfUse6sGWgJpteUkaOEGWYgR680bP9ozC6EZaqpl9eLYAYqfGGu0+4VY1q1PYjt/DVdwl5uA5PzppkHq0rhFMRMeAmgNeS2wLN5Vr/J3OVl1UNKQALyD2gYFhBZVio8Ftg/InYu8Ms4l+CtpM7SIYY5p6FARMvCiOCYnbj0iOsdJuflCdnvvVzD8XiKrUQIlVkbLgTX5iFJpPKwLfigu9ZphUiUbQk0MmzwzCg2K2GF8E2d8s6ZXjEgBsaUipI3TCfQ9gJrDxEyixpUuNfyoUfrUW32djrTs/Ia7W+rlVegj1v/4otRPTWtLHoLIvDCANUdjwDyrFzXJA+9P72R/N4sMlsNm8PvDTQ4dxheoEPGt4td4nF+YXwGw9Ft5tWMT38Rpara0LbiMWN2cAZfPtgtyND2cXsp2NCnG6pVv0qrbMH4gpE2P/KECD/zSLM7uUWhDlY53R/9PoD3l2By76gsSbgYt8IqLf5ns")]
        public string UserAccountApiToken {
            get {
                return ((string)(this["UserAccountApiToken"]));
            }
            set {
                this["UserAccountApiToken"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://api.ebay.com/wsapi")]
        public string ApiServerUrl {
            get {
                return ((string)(this["ApiServerUrl"]));
            }
            set {
                this["ApiServerUrl"] = value;
            }
        }
    }
}