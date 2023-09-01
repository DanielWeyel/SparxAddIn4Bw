using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace SparxAddIn4Bw
{
    public class EasyHelper
    {

        public String EA_Connect(EA.Repository Repository)
        {
            return "AKE";
        }

        public void EA_Disconnect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public virtual bool EA_OnPreDropFromTree(EA.Repository Repository, EA.EventProperties Info)
        {
            var eleID = Info.Get("ID");
            var diaID = Info.Get("DiagramID");
            var eletype = Info.Get("Type");
            var eleparentID = Info.Get("DroppedID");
            var x_pos = Info.Get("PositionX");
            var y_pos = Info.Get("PositionY");

            Repository.SuppressEADialogs = true;

            if (Int32.Parse(eleparentID.Value) > 0)
            {
                EA.Element ele = Repository.GetElementByID(eleID.Value);
                EA.Element parent = Repository.GetElementByID(eleparentID.Value);
                EA.Diagram dia = Repository.GetDiagramByID(diaID.Value);

                string target_stereotype = dropAs(ele.Stereotype, parent.Stereotype);




                if (target_stereotype != "false")
                {
                    Repository.SaveDiagram(diaID.Value);
                    EA.Element child = parent.Elements.AddNew("", "NAFv4-ADMBw::" + target_stereotype);
                    child.Name = ele.Name;
                    child.Notes = ele.Notes;
                    child.Multiplicity = "1";
                    child.PropertyType = ele.ElementID;
                    child.Update();
                    parent.Elements.Refresh();
                    parent.Update();

                    Repository.EnableUIUpdates = false;
                    Repository.ReloadPackage(parent.PackageID);
                    Repository.EnableUIUpdates = true;

                    EA.DiagramObject diaobj = dia.DiagramObjects.AddNew("", "");
                    diaobj.ElementID = child.ElementID;
                    diaobj.top = Int32.Parse(y_pos.Value) * -1;
                    diaobj.bottom = (Int32.Parse(y_pos.Value) * -1) - 70;
                    diaobj.left = Int32.Parse(x_pos.Value);
                    diaobj.right = Int32.Parse(x_pos.Value) + 90;
                    diaobj.Sequence = 0;
                    diaobj.Update();

                    Repository.GetDiagramByID(diaID.Value).DiagramObjects.Refresh();
                    Repository.GetDiagramByID(diaID.Value).Update();
                    Repository.ReloadDiagram(diaID.Value);
                    return false;
                }
                else if ((ele.Type == "Part" | ele.Type == "Port" | ele.Type == "StateMachine" | ele.Type == "Action"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {

                return true;
            }
            
        }

        public virtual bool EA_OnPostNewDiagramObject(EA.Repository Repository, EA.EventProperties Info)
        {
            var diaobjID = Info.Get("ID");
            var diaID = Info.Get("DiagramID");
            var diaDIUD = Info.Get("DUID");
            EA.DiagramObject diaobj = Repository.GetDiagramByID(diaID.Value).GetDiagramObjectByID(diaobjID.Value,diaDIUD.Value);
            EA.Element ele = Repository.GetElementByID(diaobj.ElementID);

            Repository.SuppressEADialogs = true;
            Repository.SaveDiagram(diaID.Value);


            if (ele.Type != "Part" & ele.Type != "Port" & ele.Type != "StateMachine")
            {


                try
                {
                    string sql;
                    XmlDocument sqlresult;
                    sql = "select t_diagramobjects.Object_ID from t_diagramobjects, t_object" +
                        " where t_diagramobjects.Object_ID <> " + diaobj.ElementID +
                        " AND t_diagramobjects.Diagram_ID = " + diaobj.DiagramID +
                        " AND t_diagramobjects.RectLeft <= " + diaobj.left +
                        " AND t_diagramobjects.RectRight >= " + diaobj.right +
                        " AND t_diagramobjects.RectTop >= " + diaobj.top +
                        " AND t_diagramobjects.RectBottom <= " + diaobj.bottom +
                        " AND t_diagramobjects.Object_ID = t_object.Object_ID" +
                        " AND t_object.Object_Type <> 'Part'" +
                        " AND t_object.Object_Type <> 'Port'";

                    sqlresult = SQLQuery(Repository, sql);
                    XmlNodeList column_Parent_Object_ID = sqlresult.GetElementsByTagName("Object_ID");
                    EA.Element parent = Repository.GetElementByID(Int32.Parse(column_Parent_Object_ID[0].InnerXml));

                    string target_stereotype = dropAs(ele.Stereotype, parent.Stereotype);
                      
                    if (target_stereotype != "false")
                    {
                        EA.Element child = parent.Elements.AddNew("", "NAFv4-ADMBw::" + target_stereotype);
                        child.Name = ele.Name;
                        child.Notes = ele.Notes;
                        child.Multiplicity = "1";
                        child.PropertyType = ele.ElementID;
                        child.Update();
                        parent.Elements.Refresh();
                        parent.Update();

                        diaobj.ElementID = child.ElementID;
                        diaobj.Sequence = 0;
                        diaobj.Update();
                        Repository.GetDiagramByID(diaID.Value).DiagramObjects.Refresh();
                        Repository.GetDiagramByID(diaID.Value).Update();
                        Repository.ReloadDiagram(diaID.Value);
                        return false;
                    }
                }

                catch (Exception e)
                {
                    return false;
                }
            }
            return false;
        } 


        public virtual bool EA_OnPostNewDiagram(EA.Repository Repository, EA.EventProperties Info)
        {
            var objID = Info.Get("DiagramID");
            EA.Diagram dia = Repository.GetDiagramByID(objID.Value);
            EA.Package pkg = Repository.GetPackageByID(dia.PackageID);
            EA.Element dianotes = pkg.Elements.AddNew("", "Text");
            pkg.Elements.Refresh();
            dianotes.Subtype = 18;
            dianotes.Update();

            EA.DiagramObject diaobj = dia.DiagramObjects.AddNew("l=26;r=200;t=-20;b=-95", "");
            diaobj.ElementID = dianotes.ElementID;
            diaobj.Update();
            dia.DiagramObjects.Refresh();

            dia.Version = "1.0";
            dia.ShowDetails = 0;
            dia.HighlightImports = false;

            //bool NAFv4 = dia.MetaType.Contains("NAFv4");

            if (dia.MetaType.Contains("NAFv4-ADMBw"))
            {
                string[] separator = { "::" };
                string string_metatype = dia.MetaType;
                string[] string_dianame = string_metatype.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
                string dianame = string_dianame[1].Replace(" - ", " : ");


                try
                {
                    string wildcard;
                    string sql;
                    XmlDocument sqlresult;

                    if (Repository.RepositoryType() == "JET")
                    {
                        wildcard = "*";
                    }
                    else
                    {
                        wildcard = "%";
                    }

                    sql = "select t_object.PDATA5 from t_object where t_object.PDATA5 LIKE '<" + wildcard + ">'";
                    sqlresult = SQLQuery(Repository, sql);
                    XmlNodeList column = sqlresult.GetElementsByTagName("PDATA5");
                    string config = column[0].InnerXml;
                    config = config.Replace("&lt;", "");   // <
                    config = config.Replace("&gt;", "");   // >
                    string[] separator_config = { "|" };
                    string[] string_config = config.Split(separator_config, System.StringSplitOptions.RemoveEmptyEntries);

                    if (Int32.Parse(string_config[1]) == 0)
                    {
                        dianame = dianame.Replace(":", ": " + string_config[0] + " :");
                        dia.Name = dianame;
                    }
                    else if (Int32.Parse(string_config[1]) == 1)
                    {
                        dianame = string_config[0] + " : " + dianame;
                        dia.Name = dianame;
                    }
                    else
                    {
                        dia.Name = string_dianame[1].Replace("-", ":");
                    }

                }
                catch
                {
                    dia.Name = string_dianame[1].Replace("-", ":");
                }
            }

            dia.Update();
            Repository.ReloadDiagram(dia.DiagramID);
            return true;
        }

        public virtual bool EA_OnPostNewElement(EA.Repository Repository, EA.EventProperties Info)
        {
            var objID = Info.Get("ElementID");
            EA.Element ele = Repository.GetElementByID(objID.Value);

            Boolean check_move = true;
            string sql;
            XmlDocument sqlresult;


            string[] arr_stereotype = {
                "OperationalRole",
                "ServiceSpecificationRole",
                "ResourceRole",
                "PostRole",
                "ProblemDomain",
                "SubOrganization",
                "TemporalPart",
                "OperationalPort",
                "ServicePort",
                "ResourcePort",
                "Measurement",
                "ConceptRole",
                "CapabilityRole",
                "VersionOfConfiguration",
                "ProjectRole",
                "ProjectMilestoneRole",
                "ProtocolLayer",
                "DataRole",
                "InformationRole",
                "OperationalStateDescription",
                "ResourceStateDescription",
                "ServiceStateDescription",
                "OperationalActivityAction",
                "ServiceFunctionAction",
                "FunctionAction"};

            //Prüfung ob Part / Port / State auch vom Stereotype beom Target zulässig ist???? 

            for (int i = 0; i < arr_stereotype.Length ; i++)
            {
                if (ele.Stereotype == arr_stereotype[i])
                {

                    if (ele.Stereotype != "Measurement" & ele.Stereotype != "ServiceFunctionAction" & ele.Stereotype != "FunctionAction")
                    {
                        ele.Multiplicity = "1";
                        ele.Update();
                    }

                    sql = "select t_diagramobjects.Object_ID, t_diagramobjects.Diagram_ID, t_diagramobjects.RectLeft," +
                            " t_diagramobjects.RectRight, t_diagramobjects.RectTop, t_diagramobjects.RectBottom" +
                            " from t_diagramobjects, t_object" +
                            " where t_object.Object_ID = " + ele.ElementID +
                            " AND t_object.Object_ID = t_diagramobjects.Object_ID";

                    sqlresult = SQLQuery(Repository, sql);
                    XmlNodeList column_Object_ID = sqlresult.GetElementsByTagName("Object_ID");
                    XmlNodeList column_Diagram_ID = sqlresult.GetElementsByTagName("Diagram_ID");
                    XmlNodeList column_RectLeft = sqlresult.GetElementsByTagName("RectLeft");
                    XmlNodeList column_RectTop = sqlresult.GetElementsByTagName("RectTop");

                    Repository.SaveDiagram(Int32.Parse(column_Diagram_ID[0].InnerXml));

                    sql = "select t_diagramobjects.Object_ID from t_diagramobjects, t_object" +
                            " where t_diagramobjects.Object_ID <> " + column_Object_ID[0].InnerXml +
                            " AND t_diagramobjects.Diagram_ID = " + column_Diagram_ID[0].InnerXml +
                            " AND t_diagramobjects.RectLeft <= " + column_RectLeft[0].InnerXml +
                            " AND t_diagramobjects.RectRight >= " + column_RectLeft[0].InnerXml +
                            " AND t_diagramobjects.RectTop >= " + column_RectTop[0].InnerXml +
                            " AND t_diagramobjects.RectBottom <= " + column_RectTop[0].InnerXml +
                            " AND t_diagramobjects.Object_ID = t_object.Object_ID" +
                            " AND t_object.Object_Type <> 'Part'" +
                            " AND t_object.Object_Type <> 'Port'";

                    sqlresult = SQLQuery(Repository, sql);
                    XmlNodeList column_Parent_Object_ID = sqlresult.GetElementsByTagName("Object_ID");
                    try
                    {
                        EA.Element parent = Repository.GetElementByID(Int32.Parse(column_Parent_Object_ID[0].InnerXml));
                        ele.ParentID = parent.ElementID;
                        ele.Name = ele.Stereotype + (parent.Elements.Count + 1).ToString();
                        ele.Update();
                        Repository.ReloadDiagram(Int32.Parse(column_Diagram_ID[0].InnerXml));
                    }
                    catch (Exception e)
                    {
                        return false;
                    }

                    check_move = false;
                    break;
                }
            }

            string[] arr_type_no = {
                "Note",
                "Constraint",
                "Text",
                "Boundary",
                "OperationalActivityAction",
                "Decision",
                "Event",
                "ActionPin",
                "ActivityPartition",
                "State",
                "StateMachine",
                "StateNode",
                "Synchronization",
                "Trigger",
                "Signal",
                "MessageEndpoint",
                "Interaction",
                "InteractionFragment",
                "InteractionState",
                "Sequence"};

            for (int i = 0; i < arr_type_no.Length; i++)
            {
                if (ele.Type == arr_type_no[i] | ele.Stereotype == arr_type_no[i])
                {
                    check_move = false;
                    break;
                }
            }

            string[] arr_type = {
                "Issue",
                "Change",
                "Requirement"};

            for (int i = 0; i < arr_type.Length; i++)
            {
                if (ele.Type == arr_type[i])
                {
                    string author_short;
                    try
                    {
                        string wildcard;

                        if (Repository.RepositoryType() == "JET")
                        {
                            wildcard = "*";
                        }
                        else
                        {
                            wildcard = "%";
                        }

                        sql = "select t_object.PDATA5 from t_object where t_object.PDATA5 LIKE '<" + wildcard + ">'";
                        sqlresult = SQLQuery(Repository, sql);
                        XmlNodeList column = sqlresult.GetElementsByTagName("PDATA5");
                        string config = column[0].InnerXml;
                        config = config.Replace("&lt;", "");   // <
                        config = config.Replace("&gt;", "");   // >
                        string[] separator_config = { "|" };
                        string[] string_config = config.Split(separator_config, System.StringSplitOptions.RemoveEmptyEntries);

                        author_short = string_config[2];
                    }
                    catch
                    {
                        author_short = "";
                    }

                    DateTime localTime = DateTime.Now;
                    string time_custom = localTime.ToString("yyyy-MM-dd"); //ISO 8601 Date Format

                    if (author_short == "")
                    {
                        ele.Name = "[" + time_custom + "] ";
                    }
                    else
                    {
                        ele.Name = "[" + time_custom + " | " + author_short + "] ";
                    }

                    ele.Update();
                    break;
                }
            }



            if (check_move == true)
            {
                try
                {
                    string wildcard;
                    if (Repository.RepositoryType() == "JET")
                    {
                        wildcard = "*";
                    }
                    else
                    {
                        wildcard = "%";
                    }
                    sql = "select t_object.ea_guid from t_object where t_object.PDATA5 LIKE '<" + wildcard + ">'";
                    sqlresult = SQLQuery(Repository, sql);
                    XmlNodeList column_mainpkg = sqlresult.GetElementsByTagName("ea_guid");
                    EA.Package elepkg = Repository.GetPackageByGuid(column_mainpkg[0].InnerXml);
                    EA.Package subpkg;

                    bool NAFv4 = ele.FQStereotype.Contains("NAFv4-ADMBw::");
                    XmlNodeList column_subpkg;

                    if (NAFv4 == true)
                    {
                        try
                        {
                            sql = "select t_object.ea_guid from t_object" +
                                  " where t_object.Name = " + "'" + ele.Stereotype + "'" +
                                  " AND t_object.Package_ID = " + elepkg.PackageID;

                            sqlresult = SQLQuery(Repository, sql);
                            column_subpkg = sqlresult.GetElementsByTagName("ea_guid");
                            subpkg = Repository.GetPackageByGuid(column_subpkg[0].InnerXml);
                            ele.PackageID = subpkg.PackageID;
                            ele.Name = ele.Stereotype + (subpkg.Elements.Count + 1).ToString();
                            ele.Update();
                            subpkg.Elements.Refresh();
                            return true;
                        }
                        catch (Exception e)
                        {
                            subpkg = elepkg.Packages.AddNew(ele.Stereotype, "");
                            subpkg.Update();
                            ele.PackageID = subpkg.PackageID;
                            ele.Update();
                            subpkg.Elements.Refresh();
                            return true;
                        }

                    }
                    else
                    {
                        try
                        {
                            sql = "select t_object.ea_guid from t_object" +
                                  " where t_object.Name = " + "'" + ele.Type + "'" +
                                  " AND t_object.Package_ID = " + elepkg.PackageID;

                            sqlresult = SQLQuery(Repository, sql);
                            column_subpkg = sqlresult.GetElementsByTagName("ea_guid");
                            subpkg = Repository.GetPackageByGuid(column_subpkg[0].InnerXml);
                            ele.PackageID = subpkg.PackageID;
                            ele.Update();
                            subpkg.Elements.Refresh();
                            return true;
                        }
                        catch (Exception e)
                        {
                            subpkg = elepkg.Packages.AddNew(ele.Type, "");
                            subpkg.Update();
                            ele.PackageID = subpkg.PackageID;
                            ele.Update();
                            subpkg.Elements.Refresh();
                            return true;
                        }
                    }
                }
                catch (Exception e)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public XmlDocument SQLQuery(EA.Repository Repository, string sqlQuery)
        {
            XmlDocument results = new XmlDocument();
            results.LoadXml(Repository.SQLQuery(sqlQuery));
            return results;
        }

        public void ReloadDiagram (EA.Repository Repository, EA.Diagram dia)
        {
            Repository.ReloadDiagram(dia.DiagramID);
        }

        private string dropAs(string stereotype, string parent)
        {
            string result;
            string search = stereotype + "_IN_" + parent;
            

            Dictionary<string, string> dropAs_dic = new Dictionary<string, string>(){

                ["ServiceSpecification_IN_ServiceSpecification"] = "ServiceSpecificationRole",
                ["ServiceInterface_IN_ServiceSpecification"] = "ServicePort",
                ["MeasurementType_IN_Capability"] = "Measurement",
                ["MeasurementType_IN_EnduringTask"] = "Measurement",
                ["MeasurementType_IN_EnterprisePhase"] = "Measurement",
                ["MeasurementType_IN_EnterpriseGoal"] = "Measurement",
                ["MeasurementType_IN_EnterpriseVision"] = "Measurement",
                ["MeasurementType_IN_ServiceSpecification"] = "Measurement",
                ["MeasurementType_IN_ProjectMilestone"] = "Measurement",
                ["MeasurementType_IN_Competence"] = "Measurement",
                ["MeasurementType_IN_Concern"] = "Measurement",
                ["MeasurementType_IN_Condition"] = "Measurement",
                ["MeasurementType_IN_ServiceInterface"] = "Measurement",
                ["MeasurementType_IN_Standard"] = "Measurement",
                ["MeasurementType_IN_WholeLifeEnterprise"] = "Measurement",
                ["MeasurementType_IN_ResourceInterface"] = "Measurement",
                ["MeasurementType_IN_Viewpoint"] = "Measurement",
                ["MeasurementType_IN_View"] = "Measurement",
                ["MeasurementType_IN_OperationalInterface"] = "Measurement",
                ["MeasurementType_IN_WholeLifeConfiguration"] = "Measurement",
                ["MeasurementType_IN_HighLevelOperationalConcept"] = "Measurement",
                ["MeasurementType_IN_InformationElement"] = "Measurement",
                ["MeasurementType_IN_GeoPoliticalExtentType"] = "Measurement",
                ["MeasurementType_IN_OperationalSignal"] = "Measurement",
                ["MeasurementType_IN_InformationRole"] = "Measurement",
                ["MeasurementType_IN_KnownResource"] = "Measurement",
                ["MeasurementType_IN_ResourceArchitecture"] = "Measurement",
                ["MeasurementType_IN_CapabilityConfiguration"] = "Measurement",
                ["MeasurementType_IN_ResourceMitigation"] = "Measurement",
                ["MeasurementType_IN_SecurityEnclave"] = "Measurement",
                ["MeasurementType_IN_System"] = "Measurement",
                ["MeasurementType_IN_NaturalResource"] = "Measurement",
                ["MeasurementType_IN_ResourceArtifact"] = "Measurement",
                ["MeasurementType_IN_Software"] = "Measurement",
                ["MeasurementType_IN_Technology"] = "Measurement",
                ["MeasurementType_IN_Project"] = "Measurement",
                ["MeasurementType_IN_Organization"] = "Measurement",
                ["MeasurementType_IN_Post"] = "Measurement",
                ["MeasurementType_IN_Person"] = "Measurement",
                ["MeasurementType_IN_Responsibility"] = "Measurement",
                ["MeasurementType_IN_OperationalPerformer"] = "Measurement",
                ["MeasurementType_IN_OperationalArchitecture"] = "Measurement",
                ["MeasurementType_IN_DataElement"] = "Measurement",
                ["ServiceSpecification_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["KnownResource_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["ResourceArchitecture_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["CapabilityConfiguration_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["ResourceMitigation_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["SecurityEnclave_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["System_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["NaturalResource_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["ResourceArtifact_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["Software_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["Technology_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["Project_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["Organization_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["Post_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["Person_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["Responsibility_IN_WholeLifeConfiguration"] = "VersionOfConfiguration",
                ["OperationalPerformer_IN_OperationalPerformer"] = "OperationalRole",
                ["OperationalArchitecture_IN_OperationalPerformer"] = "OperationalRole",
                ["KnownResource_IN_OperationalPerformer"] = "OperationalRole",
                ["OperationalPerformer_IN_OperationalArchitecture"] = "OperationalRole",
                ["OperationalArchitecture_IN_OperationalArchitecture"] = "OperationalRole",
                ["KnownResource_IN_OperationalArchitecture"] = "OperationalRole",
                ["OperationalPerformer_IN_KnownResource"] = "OperationalRole",
                ["OperationalArchitecture_IN_KnownResource"] = "OperationalRole",
                ["KnownResource_IN_KnownResource"] = "OperationalRole",
                ["Location_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["InformationElement_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["OperationalPerformer_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["OperationalArchitecture_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["DataElement_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["KnownResource_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["ResourceArchitecture_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["CapabilityConfiguration_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["ResourceMitigation_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["SecurityEnclave_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["System_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["NaturalResource_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["ResourceArtifact_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["Software_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["Technology_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["Project_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["Organization_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["Post_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["Person_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["Responsibility_IN_HighLevelOperationalConcept"] = "ConceptRole",
                ["Post_IN_Post"] = "PostRole",
                ["Post_IN_Organization"] = "PostRole",
                ["Organization_IN_Organization"] = "SubOrganization",
                ["Project_IN_Project"] = "ProjectRole",
                ["ProjectMilestone_IN_Project"] = "ProjectMilestoneRole",
                ["Capability_IN_Capability"] = "CapabilityRole",
                ["EnterprisePhase_IN_EnterprisePhase"] = "TemporalPart",
                ["EnterprisePhase_IN_WholeLifeEnterprise"] = "TemporalPart",
                ["OperationalInterface_IN_OperationalPerformer"] = "OperationalPort",
                ["OperationalInterface_IN_OperationalArchitecture"] = "OperationalPort",
                ["OperationalInterface_IN_KnownResource"] = "OperationalPort",
                ["ResourceInterface_IN_KnownResource"] = "ResourcePort",
                ["ResourceInterface_IN_ResourceArchitecture"] = "ResourcePort",
                ["ResourceInterface_IN_CapabilityConfiguration"] = "ResourcePort",
                ["ResourceInterface_IN_ResourceMitigation"] = "ResourcePort",
                ["ResourceInterface_IN_SecurityEnclave"] = "ResourcePort",
                ["ResourceInterface_IN_System"] = "ResourcePort",
                ["ResourceInterface_IN_NaturalResource"] = "ResourcePort",
                ["ResourceInterface_IN_ResourceArtifact"] = "ResourcePort",
                ["ResourceInterface_IN_Software"] = "ResourcePort",
                ["ResourceInterface_IN_Technology"] = "ResourcePort",
                ["ResourceInterface_IN_Project"] = "ResourcePort",
                ["ResourceInterface_IN_Organization"] = "ResourcePort",
                ["ResourceInterface_IN_Post"] = "ResourcePort",
                ["ResourceInterface_IN_Person"] = "ResourcePort",
                ["ResourceInterface_IN_Responsibility"] = "ResourcePort",
                ["KnownResource_IN_KnownResource"] = "ResourceRole",
                ["ResourceArchitecture_IN_KnownResource"] = "ResourceRole",
                ["CapabilityConfiguration_IN_KnownResource"] = "ResourceRole",
                ["ResourceMitigation_IN_KnownResource"] = "ResourceRole",
                ["SecurityEnclave_IN_KnownResource"] = "ResourceRole",
                ["System_IN_KnownResource"] = "ResourceRole",
                ["ResourceArtifact_IN_KnownResource"] = "ResourceRole",
                ["Software_IN_KnownResource"] = "ResourceRole",
                ["Technology_IN_KnownResource"] = "ResourceRole",
                ["KnownResource_IN_ResourceArchitecture"] = "ResourceRole",
                ["ResourceArchitecture_IN_ResourceArchitecture"] = "ResourceRole",
                ["CapabilityConfiguration_IN_ResourceArchitecture"] = "ResourceRole",
                ["ResourceMitigation_IN_ResourceArchitecture"] = "ResourceRole",
                ["SecurityEnclave_IN_ResourceArchitecture"] = "ResourceRole",
                ["System_IN_ResourceArchitecture"] = "ResourceRole",
                ["ResourceArtifact_IN_ResourceArchitecture"] = "ResourceRole",
                ["Software_IN_ResourceArchitecture"] = "ResourceRole",
                ["Technology_IN_ResourceArchitecture"] = "ResourceRole",
                ["KnownResource_IN_CapabilityConfiguration"] = "ResourceRole",
                ["ResourceArchitecture_IN_CapabilityConfiguration"] = "ResourceRole",
                ["CapabilityConfiguration_IN_CapabilityConfiguration"] = "ResourceRole",
                ["ResourceMitigation_IN_CapabilityConfiguration"] = "ResourceRole",
                ["SecurityEnclave_IN_CapabilityConfiguration"] = "ResourceRole",
                ["System_IN_CapabilityConfiguration"] = "ResourceRole",
                ["ResourceArtifact_IN_CapabilityConfiguration"] = "ResourceRole",
                ["Software_IN_CapabilityConfiguration"] = "ResourceRole",
                ["Technology_IN_CapabilityConfiguration"] = "ResourceRole",
                ["KnownResource_IN_ResourceMitigation"] = "ResourceRole",
                ["ResourceArchitecture_IN_ResourceMitigation"] = "ResourceRole",
                ["CapabilityConfiguration_IN_ResourceMitigation"] = "ResourceRole",
                ["ResourceMitigation_IN_ResourceMitigation"] = "ResourceRole",
                ["SecurityEnclave_IN_ResourceMitigation"] = "ResourceRole",
                ["System_IN_ResourceMitigation"] = "ResourceRole",
                ["ResourceArtifact_IN_ResourceMitigation"] = "ResourceRole",
                ["Software_IN_ResourceMitigation"] = "ResourceRole",
                ["Technology_IN_ResourceMitigation"] = "ResourceRole",
                ["KnownResource_IN_SecurityEnclave"] = "ResourceRole",
                ["ResourceArchitecture_IN_SecurityEnclave"] = "ResourceRole",
                ["CapabilityConfiguration_IN_SecurityEnclave"] = "ResourceRole",
                ["ResourceMitigation_IN_SecurityEnclave"] = "ResourceRole",
                ["SecurityEnclave_IN_SecurityEnclave"] = "ResourceRole",
                ["System_IN_SecurityEnclave"] = "ResourceRole",
                ["ResourceArtifact_IN_SecurityEnclave"] = "ResourceRole",
                ["Software_IN_SecurityEnclave"] = "ResourceRole",
                ["Technology_IN_SecurityEnclave"] = "ResourceRole",
                ["KnownResource_IN_System"] = "ResourceRole",
                ["ResourceArchitecture_IN_System"] = "ResourceRole",
                ["CapabilityConfiguration_IN_System"] = "ResourceRole",
                ["ResourceMitigation_IN_System"] = "ResourceRole",
                ["SecurityEnclave_IN_System"] = "ResourceRole",
                ["System_IN_System"] = "ResourceRole",
                ["ResourceArtifact_IN_System"] = "ResourceRole",
                ["Software_IN_System"] = "ResourceRole",
                ["Technology_IN_System"] = "ResourceRole",
                ["KnownResource_IN_ResourceArtifact"] = "ResourceRole",
                ["ResourceArchitecture_IN_ResourceArtifact"] = "ResourceRole",
                ["CapabilityConfiguration_IN_ResourceArtifact"] = "ResourceRole",
                ["ResourceMitigation_IN_ResourceArtifact"] = "ResourceRole",
                ["SecurityEnclave_IN_ResourceArtifact"] = "ResourceRole",
                ["System_IN_ResourceArtifact"] = "ResourceRole",
                ["ResourceArtifact_IN_ResourceArtifact"] = "ResourceRole",
                ["Software_IN_ResourceArtifact"] = "ResourceRole",
                ["Technology_IN_ResourceArtifact"] = "ResourceRole",
                ["KnownResource_IN_Software"] = "ResourceRole",
                ["ResourceArchitecture_IN_Software"] = "ResourceRole",
                ["CapabilityConfiguration_IN_Software"] = "ResourceRole",
                ["ResourceMitigation_IN_Software"] = "ResourceRole",
                ["SecurityEnclave_IN_Software"] = "ResourceRole",
                ["System_IN_Software"] = "ResourceRole",
                ["ResourceArtifact_IN_Software"] = "ResourceRole",
                ["Software_IN_Software"] = "ResourceRole",
                ["Technology_IN_Software"] = "ResourceRole",
                ["KnownResource_IN_Technology"] = "ResourceRole",
                ["ResourceArchitecture_IN_Technology"] = "ResourceRole",
                ["CapabilityConfiguration_IN_Technology"] = "ResourceRole",
                ["ResourceMitigation_IN_Technology"] = "ResourceRole",
                ["SecurityEnclave_IN_Technology"] = "ResourceRole",
                ["System_IN_Technology"] = "ResourceRole",
                ["ResourceArtifact_IN_Technology"] = "ResourceRole",
                ["Software_IN_Technology"] = "ResourceRole",
                ["Technology_IN_Technology"] = "ResourceRole",
                ["InformationElement_IN_InformationElement"] = "InformationRole",
                ["InformationElement_IN_OperationalPerformer"] = "InformationRole",
                ["InformationElement_IN_OperationalArchitecture"] = "InformationRole",
                ["InformationElement_IN_KnownResource"] = "InformationRole",
                ["DataElement_IN_DataElement"] = "DataRole",
                ["DataElement_IN_KnownResource"] = "DataRole",
                ["DataElement_IN_ResourceArchitecture"] = "DataRole",
                ["DataElement_IN_CapabilityConfiguration"] = "DataRole",
                ["DataElement_IN_ResourceMitigation"] = "DataRole",
                ["DataElement_IN_SecurityEnclave"] = "DataRole",
                ["DataElement_IN_System"] = "DataRole",
                ["DataElement_IN_NaturalResource"] = "DataRole",
                ["DataElement_IN_ResourceArtifact"] = "DataRole",
                ["DataElement_IN_Software"] = "DataRole",
                ["DataElement_IN_Technology"] = "DataRole",
                ["DataElement_IN_Project"] = "DataRole",
                ["DataElement_IN_Organization"] = "DataRole",
                ["DataElement_IN_Post"] = "DataRole",
                ["DataElement_IN_Person"] = "DataRole",
                ["DataElement_IN_Responsibility"] = "DataRole",
                ["Protocol_IN_Protocolstack"] = "ProtocolLayer",
            };

            if (dropAs_dic.ContainsKey(search) == true)
            {
                result = dropAs_dic[search];
            }
            else
            {
                result = "false";
            }

            return result;
        }

    }
}
