﻿using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Editor;
using ESRI.ArcGIS.Geodatabase;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UtransEditorAGRC
{
    public partial class frmUtransEditor : Form
    {
        //form-wide variables...
        // create a list of controls that contains address pieces for managing edits
        private List<Control> ctrlList = new List<Control>();

        string txtUtransExistingL_F_Add;
        string txtUtransExistingL_TAdd;
        string txtUtransExistingR_F_Add;
        string txtUtransExistingR_T_Add;
        string txtUtransExistingPreDir;
        string txtUtransExistingStName;
        string txtUtransExistingStType;
        string txtUtransExistingSufDir;
        string txtUtransExistingAlias1;
        string txtUtransExistingAlias1Type;
        string txtUtransExistingAlias2;
        string txtUtransExistingAlias2Type;
        string txtUtransExistingAcsAlias;
        string txtUtransExistingAscSuf;


        //get the selected feature(s) from the dfc fc
        IFeatureSelection arcFeatureSelection; // = clsGlobals.arcGeoFLayerDfcResult as IFeatureSelection;
        ISelectionSet arcSelSet; // = arcFeatureSelection.SelectionSet;



        public frmUtransEditor()
        {
            InitializeComponent();
        }


        //form load event
        private void frmUtransEditor_Load(object sender, EventArgs e)
        {
            try
            {
                //setup event handler for when the  map selection changes
                ((IEditEvents_Event)clsGlobals.arcEditor).OnSelectionChanged += new IEditEvents_OnSelectionChangedEventHandler(frmUtransEditor_OnSelectionChanged);

                //get the editor workspace
                IWorkspace arcWspace = clsGlobals.arcEditor.EditWorkspace;

                //if the workspace is not remote (sde), exit the sub - if it's sde get the version name
                if (arcWspace.Type != esriWorkspaceType.esriRemoteDatabaseWorkspace) 
                { 
                    return; 
                }
                else
                {
                    //IVersionedWorkspace versionedWorkspace = (IVersionedWorkspace)arcWspace;
                    IVersion2 arcVersion = (IVersion2)arcWspace;

                    lblVersionName.Text = arcVersion.VersionName.ToString();

                    //show message box so user knows what version they are editing on the utrans database
                    MessageBox.Show("You are editing the UTRANS database using the following version: " + arcVersion.VersionName, "Utrans Version", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                //get the workspace as an IWorkspaceEdit
                IWorkspaceEdit arcWspaceEdit = clsGlobals.arcEditor.EditWorkspace as IWorkspaceEdit;

                //get the workspace as a feature workspace
                IFeatureWorkspace arcFeatWspace = arcWspace as IFeatureWorkspace;

                //get the current document
                IMxDocument arcMxDoc = clsGlobals.arcApplication.Document as IMxDocument;

                //get the focus map
                IMap arcMapp = arcMxDoc.FocusMap;

                IActiveView arcActiveView = arcMapp as IActiveView;

                //get reference to the layers in the map
                //clear out any reference to the utrans street layer
                clsGlobals.arcGeoFLayerUtransStreets = null;

                //loop through the map layers and get the utrans.statewidestreets, the county roads data, and the detect feature change fc - all into IGeoFeatureLayer(s)
                for (int i = 0; i < arcMapp.LayerCount; i++)
                {
                    if (arcMapp.get_Layer(i) is IGeoFeatureLayer)
                    {
                        try
                        {
                            IFeatureLayer arcFLayer = arcMapp.get_Layer(i) as IFeatureLayer;
                            IFeatureClass arcFClass = arcFLayer.FeatureClass;
                            IObjectClass arcObjClass = arcFClass as IObjectClass;
                            if (arcObjClass.AliasName.ToString().ToUpper() == "UTRANS.TRANSADMIN.STATEWIDESTREETS")
                            {
                                clsGlobals.arcGeoFLayerUtransStreets = arcMapp.get_Layer(i) as IGeoFeatureLayer;
                                //MessageBox.Show("referenced utrans streets");
                            }
                            if (arcObjClass.AliasName.ToString().ToUpper() == "COUNTY_STREETS")
                            {
                                clsGlobals.arcGeoFLayerCountyStreets = arcMapp.get_Layer(i) as IGeoFeatureLayer;
                                //MessageBox.Show("referenced county streets");
                            }
                            if (arcObjClass.AliasName.ToString().ToUpper() == "DFC_RESULT")
                            {
                                clsGlobals.arcGeoFLayerDfcResult = arcMapp.get_Layer(i) as IGeoFeatureLayer;
                                //MessageBox.Show("referenced dfc results");
                            }
                        }

                        catch (Exception) { }//in case there is an error looping through layers (sometimes on group layers or dynamic xy layers), just keep going

                    }
                }

                //shouldn't need this code as i've changed the code to check for these layers before i enable the button
                //check that the needed layers are in the map - if not, show message and close the form
                if (clsGlobals.arcGeoFLayerCountyStreets == null)
                {
                    MessageBox.Show("A needed layer is Missing in the map.   Please add 'COUNTYSTREETS' in order to continue.", "Missing Layer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }
                else if (clsGlobals.arcGeoFLayerDfcResult == null)
                {
                    MessageBox.Show("A needed layer is Missing in the map.   Please add 'DFC_RESULT' in order to continue.", "Missing Layer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }
                else if (clsGlobals.arcGeoFLayerUtransStreets == null)
                {
                    MessageBox.Show("A needed layer is Missing in the map.   Please add 'UTRANS.TRANSADMIN.STATEWIDESTREETS' in order to continue.", "Missing Layer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                
                //clear the selection in the map, so we can start fresh with the tool and user's inputs
                arcMapp.ClearSelection();
                
                //refresh the map on the selected features
                arcActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);


                //add textboxes to the control list
                ctrlList.Add(this.txtCountyAcsAlilas);
                ctrlList.Add(this.txtCountyAcsSuf);
                ctrlList.Add(this.txtCountyAlias1);
                ctrlList.Add(this.txtCountyAlias1Type);
                ctrlList.Add(this.txtCountyAlias2);
                ctrlList.Add(this.txtCountyAlias2Type);
                ctrlList.Add(this.txtCountyL_F_Add);
                ctrlList.Add(this.txtCountyL_T_Add);
                ctrlList.Add(this.txtCountyPreDir);
                ctrlList.Add(this.txtCountyR_F_Add);
                ctrlList.Add(this.txtCountyR_T_Add);
                ctrlList.Add(this.txtCountyStName);
                ctrlList.Add(this.txtCountyStType);
                ctrlList.Add(this.txtCountySufDir);
                ctrlList.Add(this.txtUtranL_F_Add);
                ctrlList.Add(this.txtUtranL_T_Add);
                ctrlList.Add(this.txtUtranPreDir);
                ctrlList.Add(this.txtUtranR_F_Add);
                ctrlList.Add(this.txtUtranR_T_Add);
                ctrlList.Add(this.txtUtransAcsAllias);
                ctrlList.Add(this.txtUtransAcsSuf);
                ctrlList.Add(this.txtUtransAlias1);
                ctrlList.Add(this.txtUtransAlias1Type);
                ctrlList.Add(this.txtUtransAlias2);
                ctrlList.Add(this.txtUtransAlias2Type);
                ctrlList.Add(this.txtUtranStName);
                ctrlList.Add(this.txtUtranStType);
                ctrlList.Add(this.txtUtranSufDir);


                //make sure the backcolor of each color is white
                for (int i = 0; i < ctrlList.Count; i++)
                {
                    Control ctrl = ctrlList.ElementAt(i);
                    ctrl.BackColor = Color.White;
                    ctrl.Text = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //throw;
            }
        }




        //this event is called when the selection changes in the map
        private void frmUtransEditor_OnSelectionChanged()
        {
            try
            {
                //moved to top for wider scope
                //get the selected feature(s) from the dfc fc
                //IFeatureSelection arcFeatureSelection = clsGlobals.arcGeoFLayerDfcResult as IFeatureSelection;
                //ISelectionSet arcSelSet = arcFeatureSelection.SelectionSet;

                //make sure the backcolor of each color is white
                for (int i = 0; i < ctrlList.Count; i++)
                {
                    Control ctrl = ctrlList.ElementAt(i);
                    ctrl.BackColor = Color.White;
                    ctrl.Text = "";
                }

                //revert labels back to regular (non-italic)
                //create an italic font for lables - to use where data does not match
                Font fontLabelRegular = new Font("Microsoft Sans Serif", 8.0f, FontStyle.Regular);
                lblAcsAlias.Font = fontLabelRegular;
                lblAcsSuf.Font = fontLabelRegular;
                lblAlias.Font = fontLabelRegular;
                lblAlias1Type.Font = fontLabelRegular;
                lblAlias2.Font = fontLabelRegular;
                lblAlias2Type.Font = fontLabelRegular;
                lblLeftFrom.Font = fontLabelRegular;
                lblLeftTo.Font = fontLabelRegular;
                lblPreDir.Font = fontLabelRegular;
                lblRightFrom.Font = fontLabelRegular;
                lblRightTo.Font = fontLabelRegular;
                lblStName.Font = fontLabelRegular;
                lblStType.Font = fontLabelRegular;
                lblSufDir.Font = fontLabelRegular;

                //clear utrans existing variables - for reuse
                txtUtransExistingL_F_Add = null;
                txtUtransExistingL_F_Add = txtUtranL_F_Add.Text;
                txtUtransExistingL_TAdd = null;
                txtUtransExistingL_TAdd = txtUtranL_T_Add.Text;
                txtUtransExistingR_F_Add = null;
                txtUtransExistingR_F_Add = txtUtranR_F_Add.Text;  //finish these and then handle the bold font if the text in the boxes changed from original, also make both green if match
                txtUtransExistingR_T_Add = null;
                txtUtransExistingPreDir = null;
                txtUtransExistingStName = null;
                txtUtransExistingStType = null;
                txtUtransExistingSufDir = null;
                txtUtransExistingAlias1 = null;
                txtUtransExistingAlias1Type = null;
                txtUtransExistingAlias2 = null;
                txtUtransExistingAlias2Type = null;
                txtUtransExistingAcsAlias = null;
                txtUtransExistingAscSuf = null;


                arcFeatureSelection = clsGlobals.arcGeoFLayerDfcResult as IFeatureSelection;
                arcSelSet = arcFeatureSelection.SelectionSet;


                //check record is selected in the dfc fc
                if (arcSelSet.Count == 1)
                {
                    //get a cursor of the selected features
                    ICursor arcCursor;
                    arcSelSet.Search(null, false, out arcCursor);

                    //get the first row (there should only be one)
                    IRow arcRow = arcCursor.NextRow();

                    //get the objectids from dfc layer for selecting on corresponding layer
                    string strCountyOID = arcRow.get_Value(arcRow.Fields.FindField("UPDATE_FID")).ToString();
                    string strUtransOID = arcRow.get_Value(arcRow.Fields.FindField("BASE_FID")).ToString();
                    string strChangeType = arcRow.get_Value(arcRow.Fields.FindField("CHANGE_TYPE")).ToString();

                    //populate the change type on the top of the form
                    switch (strChangeType)
                    {
                        case "N":
                            lblChangeType.Text = "New";
                            break;
                        case "S":
                            lblChangeType.Text = "Spatial";
                            break;
                        case "A":
                            lblChangeType.Text = "Attribute";
                            break;
                        case "SA":
                            lblChangeType.Text = "Spatial and Attribute";
                            break;
                        case "NC":
                            lblChangeType.Text = "No Change";
                            break;
                        case "D":
                            lblChangeType.Text = "Delation";
                            break;
                        default:
                            lblChangeType.Text = "Unknown";
                            break;
                    }


                    //get the corresponding features
                    IQueryFilter arcCountyQueryFilter = new QueryFilter();
                    arcCountyQueryFilter.WhereClause = "OBJECTID = " + strCountyOID.ToString();
                    //MessageBox.Show("County OID: " + strCountyOID.ToString());

                    IQueryFilter arcUtransQueryFilter = new QueryFilter();
                    arcUtransQueryFilter.WhereClause = "OBJECTID = " + strUtransOID.ToString();
                    //can check if oid = -1 then it's a new record so maybe make backround color on form green or something until user says okay to import, then populate
                    //MessageBox.Show("Utrans OID: " + strUtransOID.ToString());

                    IFeatureCursor arcCountyFeatCursor = clsGlobals.arcGeoFLayerCountyStreets.Search(arcCountyQueryFilter, true);
                    IFeature arcCountyFeature = (IFeature)arcCountyFeatCursor.NextFeature();


                    IFeatureCursor arcUtransFeatCursor = clsGlobals.arcGeoFLayerUtransStreets.Search(arcUtransQueryFilter, true);
                    IFeature arcUtransFeature = (IFeature)arcUtransFeatCursor.NextFeature();


                    //update the textboxes with the selected dfc row//

                    //make sure the query returned results for county roads
                    if (arcCountyFeature != null)
                    {
                        //update all the text boxes
                        foreach (var ctrl in ctrlList)
                        {
                            if (arcCountyFeature.Fields.FindFieldByAliasName(ctrl.Tag.ToString()) > -1)
                            {
                                ctrl.Text = arcCountyFeature.get_Value(arcCountyFeature.Fields.FindFieldByAliasName(ctrl.Tag.ToString())).ToString().ToUpper();
                            }
                        }
                    }


                    //make sure the query returned results for utrans roads
                    if (arcUtransFeature != null)
                    {
                        //update all the text boxes
                        foreach (var ctrl in ctrlList)
                        {
                            if (arcUtransFeature.Fields.FindFieldByAliasName(ctrl.Tag.ToString())>-1)
                            {
                                ctrl.Text = arcUtransFeature.get_Value(arcUtransFeature.Fields.FindFieldByAliasName(ctrl.Tag.ToString())).ToString();
                            }
                        }
                    }


                    //call check differnces method
                    checkTextboxDifferneces();

                }
                else //if the user selects more or less than one record in the dfc fc - clear out the textboxes
                {
                    //clear out the textboxes so nothing is populated
                    foreach (var ctrl in ctrlList)
                    {
                        ctrl.Text = "";
                    }
                }
 


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " " + ex.Source + " " + ex.StackTrace + " " + ex.TargetSite, "Error!");
            }
        }



        //this method updates the textbox controls on the form with the currently selected features
        private void updateTextBox(Control ctrl, string strCountyObjectID, string strUtransObjecID) 
        {

            try
            {
                //this.Focus();//set focus to the form if there is a road segment selected

                ////get a cursor of the selected features
                //ICursor arcCursor;
                //arcSelSet.Search(null, false, out arcCursor);

                ////get the first row (there should only be one)
                //IRow arcRow = arcCursor.NextRow();

                ////get the objectids from dfc layer for selecting on corresponding layer
                //string strCountyOID = arcRow.get_Value(arcRow.Fields.FindField("UPDATE_FID")).ToString();
                //string strUtransOID = arcRow.get_Value(arcRow.Fields.FindField("BASE_FID")).ToString();
                //string strChangeType = arcRow.get_Value(arcRow.Fields.FindField("CHANGE_TYPE")).ToString();

                ////populate the change type on the top of the form
                //switch (strChangeType)
                //{
                //    case "N":
                //        lblChangeType.Text = "New";
                //        break;
                //    case "S":
                //        lblChangeType.Text = "Spatial";
                //        break;
                //    case "A":
                //        lblChangeType.Text = "Attribute";
                //        break;
                //    case "SA":
                //        lblChangeType.Text = "Spatial and Attribute";
                //        break;
                //    case "NC":
                //        lblChangeType.Text = "No Change";
                //        break;
                //    case "D":
                //        lblChangeType.Text = "Delation";
                //        break;
                //    default:
                //        lblChangeType.Text = "Unknown";
                //        break;
                //}

                ////get the corresponding features
                //IQueryFilter arcCountyQueryFilter = new QueryFilter();
                //arcCountyQueryFilter.WhereClause = "OBJECTID = " + strCountyObjectID.ToString();

                //IQueryFilter arcUtransQueryFilter = new QueryFilter();
                //arcUtransQueryFilter.WhereClause = "OBJECTID = " + strUtransObjecID.ToString();


                //IFeatureCursor arcCountyFeatCursor = clsGlobals.arcGeoFLayerCountyStreets.Search(arcCountyQueryFilter, true);
                //arcCountyFeatCursor.NextFeature();
                

                //IFeatureCursor arcUtransFeatCursor = clsGlobals.arcGeoFLayerUtransStreets.Search(arcUtransQueryFilter, true);
                //arcUtransFeatCursor.NextFeature();

                //if (arcCountyFeatCursor != null)
                //{
                    

                //}

                //if (arcUtransFeatCursor != null)
                //{
                    

                //}



            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " " + ex.Source + " " + ex.StackTrace + " " + ex.TargetSite, "Error!");
            }
        }



        private void checkTextboxDifferneces() 
        {
            try
            {
                //create an italic font for lables - to use where data does not match
                Font fontLabelDataMismatch = new Font("Microsoft Sans Serif", 8.0f, FontStyle.Bold);

                if (txtCountyStName.Text.ToUpper().ToString() != txtUtranStName.Text.ToUpper().ToString())
                {
                    txtUtranStName.BackColor = Color.LightSalmon;
                    txtCountyStName.BackColor = Color.LightSalmon;
                    lblStName.Font = fontLabelDataMismatch;
                }
                if (txtCountyStType.Text.ToUpper().ToString() != txtUtranStType.Text.ToUpper().ToString())
                {
                    txtUtranStType.BackColor = Color.LightSalmon;
                    txtCountyStType.BackColor = Color.LightSalmon;
                    lblStType.Font = fontLabelDataMismatch;
                }
                if (txtCountySufDir.Text.ToUpper().ToString() != txtUtranSufDir.Text.ToUpper().ToString())
                {
                    txtUtranSufDir.BackColor = Color.LightSalmon;
                    txtCountySufDir.BackColor = Color.LightSalmon;
                    lblSufDir.Font = fontLabelDataMismatch;
                }
                if (txtCountyPreDir.Text.ToUpper().ToString() != txtUtranPreDir.Text.ToUpper().ToString())
                {
                    txtUtranPreDir.BackColor = Color.LightSalmon;
                    txtCountyPreDir.BackColor = Color.LightSalmon;
                    lblPreDir.Font = fontLabelDataMismatch;
                }
                if (txtCountyL_F_Add.Text.ToString() != txtUtranL_F_Add.Text.ToString())
                {
                    txtUtranL_F_Add.BackColor = Color.LightSalmon;
                    txtCountyL_F_Add.BackColor = Color.LightSalmon;
                    lblLeftFrom.Font = fontLabelDataMismatch;
                    //capture the curent text - incase we want to revert to it
                    //txtUtransExistingL_F_Add = txtUtranL_F_Add.Text;
                }
                if (txtCountyL_T_Add.Text.ToString() != txtUtranL_T_Add.Text.ToString())
                {
                    txtUtranL_T_Add.BackColor = Color.LightSalmon;
                    txtCountyL_T_Add.BackColor = Color.LightSalmon;
                    lblLeftTo.Font = fontLabelDataMismatch;
                }
                if (txtCountyR_F_Add.Text.ToString() != txtUtranR_F_Add.Text.ToString())
                {
                    txtUtranR_F_Add.BackColor = Color.LightSalmon;
                    txtCountyR_F_Add.BackColor = Color.LightSalmon;
                    lblRightFrom.Font = fontLabelDataMismatch;
                }
                if (txtCountyR_T_Add.Text.ToString() != txtUtranR_T_Add.Text.ToString())
                {
                    txtUtranR_T_Add.BackColor = Color.LightSalmon;
                    txtCountyR_T_Add.BackColor = Color.LightSalmon;
                    lblRightTo.Font = fontLabelDataMismatch;
                }
                if (txtCountyAcsAlilas.Text.ToUpper().ToString() != txtUtransAcsAllias.Text.ToUpper().ToString())
                {
                    txtUtransAcsAllias.BackColor = Color.LightSalmon;
                    txtCountyAcsAlilas.BackColor = Color.LightSalmon;
                    lblAcsAlias.Font = fontLabelDataMismatch;
                }
                if (txtCountyAcsSuf.Text.ToUpper().ToString() != txtUtransAcsSuf.Text.ToUpper().ToString())
                {
                    txtUtransAcsSuf.BackColor = Color.LightSalmon;
                    txtCountyAcsSuf.BackColor = Color.LightSalmon;
                    lblAcsSuf.Font = fontLabelDataMismatch;
                }
                if (txtCountyAlias1.Text.ToUpper().ToString() != txtUtransAlias1.Text.ToUpper().ToString())
                {
                    txtUtransAlias1.BackColor = Color.LightSalmon;
                    txtCountyAlias1.BackColor = Color.LightSalmon;
                    lblAlias.Font = fontLabelDataMismatch;
                }
                if (txtCountyAlias1Type.Text.ToUpper().ToString() != txtUtransAlias1Type.Text.ToUpper().ToString())
                {
                    txtUtransAlias1Type.BackColor = Color.LightSalmon;
                    txtCountyAlias1Type.BackColor = Color.LightSalmon;
                    lblAlias1Type.Font = fontLabelDataMismatch;
                }
                if (txtCountyAlias2.Text.ToUpper().ToString() != txtUtransAlias2.Text.ToUpper().ToString())
                {
                    txtUtransAlias2.BackColor = Color.LightSalmon;
                    txtCountyAlias2.BackColor = Color.LightSalmon;
                    lblAlias2.Font = fontLabelDataMismatch;
                }
                if (txtCountyAlias2Type.Text.ToUpper().ToString() != txtUtransAlias2.Text.ToUpper().ToString())
                {
                    txtUtransAlias2.BackColor = Color.LightSalmon;
                    txtCountyAlias2.BackColor = Color.LightSalmon;
                    lblAlias2Type.Font = fontLabelDataMismatch;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " " + ex.Source + " " + ex.StackTrace + " " + ex.TargetSite, "Error!");
            }
        
        }



        //open a hyper link to show the google doc describing the attributes for the utrans schema
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                //open google doc attr doc showing attribute details
                //System.Diagnostics.Process.Start(e.Link.LinkData as string);
                System.Diagnostics.Process.Start("https://docs.google.com/document/d/1ojjqCa1Z6IG6Wj0oAbZatoYsmbKzO9XwdD88-kqm-zQ/edit");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //throw;
            }




        }






        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                //cboStatusField.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //throw;
            }
        }


        private void cboStatusField_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                //cboStatusField.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                //throw;
            }

        }


        //the following methods handle the double click labels
        private void lblLeftFrom_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                Label clickedLabel = sender as Label;

                if (clickedLabel.Text == "L_F_ADD")
                {
                    if (txtUtranL_F_Add.BackColor == Color.LightSalmon)
                    {
                        txtUtranL_F_Add.BackColor = Color.LightSeaGreen;
                        txtUtranL_F_Add.Text = txtCountyL_F_Add.Text;
                        txtUtranL_F_Add.ReadOnly = true;
                        return;
                    }
                    if (txtUtranL_F_Add.BackColor == Color.LightSeaGreen)
                    {
                        txtUtranL_F_Add.BackColor = Color.White;
                        txtUtranL_F_Add.ReadOnly = false;
                        return;
                    }
                    if (txtUtranL_F_Add.BackColor == Color.White)
                    {
                        txtUtranL_F_Add.BackColor = Color.LightSalmon;
                        txtUtranL_F_Add.Text = txtUtransExistingL_F_Add;
                        txtUtranL_F_Add.ReadOnly = true;
                        return;
                    }
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Message: " + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                "Error Source: " + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine +
                "Error Location:" + Environment.NewLine + ex.StackTrace,
                "UTRANS Editor tool error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }


        private void lblLeftTo_DoubleClick(object sender, EventArgs e)
        {

        }

        private void lblRightFrom_DoubleClick(object sender, EventArgs e)
        {

        } 

        private void lblRightTo_DoubleClick(object sender, EventArgs e)
        {

        }

        private void lblPreDir_DoubleClick(object sender, EventArgs e)
        {

        }

        private void lblStName_DoubleClick(object sender, EventArgs e)
        {

        }

        private void lblStType_DoubleClick(object sender, EventArgs e)
        {

        }

        private void lblSufDir_DoubleClick(object sender, EventArgs e)
        {

        }

        private void lblAlias_DoubleClick(object sender, EventArgs e)
        {

        }

        private void lblAlias1Type_DoubleClick(object sender, EventArgs e)
        {

        }

        private void lblAlias2_DoubleClick(object sender, EventArgs e)
        {

        }

        private void lblAlias2Type_DoubleClick(object sender, EventArgs e)
        {

        }

        private void lblAcsAlias_DoubleClick(object sender, EventArgs e)
        {

        }

        private void lblAcsSuf_DoubleClick(object sender, EventArgs e)
        {

        }



        


























    }
}
