﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AccountingHelper.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.10.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Users\\franz\\OneDrive - Franz Bichlmaier Consulting\\QuantCo")]
        public string RootDirectory {
            get {
                return ((string)(this["RootDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Datev")]
        public string DatevDirectory {
            get {
                return ((string)(this["DatevDirectory"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Reisekosten")]
        public string TravelExpenseDirectory {
            get {
                return ((string)(this["TravelExpenseDirectory"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("bichlmaier@quantco.com")]
        public string EmailAccount {
            get {
                return ((string)(this["EmailAccount"]));
            }
            set {
                this["EmailAccount"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("diana.schmidt@bdo.de")]
        public string BdoTo {
            get {
                return ((string)(this["BdoTo"]));
            }
            set {
                this["BdoTo"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("guenter.wagner@bdo.de")]
        public string BdoCc {
            get {
                return ((string)(this["BdoCc"]));
            }
            set {
                this["BdoCc"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("stefan.bahr@bdo.de;diana.schmidt@bdo.de")]
        public string BdoHr {
            get {
                return ((string)(this["BdoHr"]));
            }
            set {
                this["BdoHr"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IsTest {
            get {
                return ((bool)(this["IsTest"]));
            }
            set {
                this["IsTest"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("EmployeeDocuments")]
        public string EmployeeDocuments {
            get {
                return ((string)(this["EmployeeDocuments"]));
            }
            set {
                this["EmployeeDocuments"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("blauth@quantco.com")]
        public string CEOTo {
            get {
                return ((string)(this["CEOTo"]));
            }
            set {
                this["CEOTo"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Users\\franz\\OneDrive - Franz Bichlmaier Consulting\\QuantCo\\pdfVorlage.pdf")]
        public string PdfVorlagePortait {
            get {
                return ((string)(this["PdfVorlagePortait"]));
            }
            set {
                this["PdfVorlagePortait"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Users\\franz\\OneDrive - Franz Bichlmaier Consulting\\QuantCo\\ContractTemplates")]
        public string ContractTemplates {
            get {
                return ((string)(this["ContractTemplates"]));
            }
            set {
                this["ContractTemplates"] = value;
            }
        }
    }
}
