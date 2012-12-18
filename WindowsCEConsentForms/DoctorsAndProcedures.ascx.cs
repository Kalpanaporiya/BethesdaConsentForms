﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsCEConsentForms.FormHandlerService;

namespace WindowsCEConsentForms
{
    public partial class DoctorsAndProcedures : System.Web.UI.UserControl
    {
        public ConsentType ConsentType;

        public bool IsStaticTextBoxForPrecedures;

        protected void Page_Load(object sender, EventArgs e)
        {
            var formHandlerServiceClient = new FormHandlerServiceClient();
            if (!IsPostBack)
            {
                var procedures = new List<string>();
                if (!IsStaticTextBoxForPrecedures)
                {
                    if (ConsentType == ConsentType.Endoscopy)
                        procedures.AddRange(formHandlerServiceClient.GetEndoscopyProcedurenameList());
                    else if (ConsentType == ConsentType.Cardiovascular)
                        procedures.AddRange(formHandlerServiceClient.GetCardiovascularProcedurenameList());
                    else
                        procedures.AddRange(formHandlerServiceClient.GetProcedurenameList());
                    procedures.Add("Other");
                }

                ViewState["ListOfProcedures"] = procedures;

                var primaryDoctors = new List<PrimaryDoctor> { new PrimaryDoctor { Id = 0, Name = "----Select Primary Doctor----" } };
                var physicians = formHandlerServiceClient.GetPrimaryPhysiciansList();
                if (physicians != null)
                {
                    primaryDoctors.AddRange(from DataRow row in physicians.Rows select new PrimaryDoctor { Name = row["Lname"] + ", " + row["Fname"], Id = int.Parse(row["PhysicianId"].ToString()) });
                }

                ViewState["PrimaryDoctors"] = primaryDoctors;
                var doctorsProceduresState = new DoctorsProceduresState
                {
                    SelectedDoctorsIndex = new[] { "0" },
                    SelectedProcedures = new[] { "" }
                };
                ViewState["DoctorsProceduresState"] = doctorsProceduresState;
            }
            else
            {
                var doctorsProceduresState = new DoctorsProceduresState
                                                 {
                                                     SelectedDoctorsIndex = Request.Form["DdlPrimaryDoctors"].Split(','),
                                                     SelectedProcedures =
                                                         IsStaticTextBoxForPrecedures
                                                             ? Request.Form["TxtProcedures"].Split(',')
                                                             : Request.Form["HdnSelectedProcedures"].Split(','),
                                                 };
                ViewState["DoctorsProceduresState"] = doctorsProceduresState;
            }
        }

        public bool SaveDoctorsAndProcedures(FormHandlerServiceClient formHandlerServiceClient, string patientId)
        {
            var outPut = new List<DoctorAndProcedure>();
            if (IsStaticTextBoxForPrecedures)
            {
                int index = 0;
                string[] primaryDoctors = Request.Form["DdlPrimaryDoctors"].Split(',');
                foreach (string procedure in Request.Form["TxtProcedures"].Split(','))
                {
                    if (primaryDoctors.GetUpperBound(0) > index - 1)
                    {
                        if (!string.IsNullOrEmpty(primaryDoctors[index]) && !string.IsNullOrEmpty(procedure))
                        {
                            outPut.Add(new DoctorAndProcedure { _primaryDoctorId = primaryDoctors[index], _precedures = procedure });
                        }
                        index++;
                    }
                    else
                        break;
                }
            }
            else
            {
                int index = 0;
                string[] primaryDoctors = Request.Form["DdlPrimaryDoctors"].Split(',');
                string[] otherProcedures = Request.Form["TxtOtherProcedure"].Split(',');
                foreach (string procedure in Request.Form["HdnSelectedProcedures"].Split(','))
                {
                    if (primaryDoctors.GetUpperBound(0) > index - 1)
                    {
                        if (!string.IsNullOrEmpty(primaryDoctors[index]) && !string.IsNullOrEmpty(procedure))
                        {
                            if (procedure.IndexOf("Other", StringComparison.Ordinal) > 0 && otherProcedures.GetUpperBound(0) > index - 1)
                                outPut.Add(new DoctorAndProcedure { _primaryDoctorId = primaryDoctors[index], _precedures = procedure.Replace("#Other", "#" + otherProcedures[index]) });
                            else
                                outPut.Add(new DoctorAndProcedure { _primaryDoctorId = primaryDoctors[index], _precedures = procedure });
                        }
                        index++;
                    }
                    else
                        break;
                }
            }
            if (outPut.Count == 0 || outPut.Any(doctorAndProcedure => doctorAndProcedure._precedures == string.Empty))
                return false;
            formHandlerServiceClient.SaveDoctorsDetails(patientId, ConsentType.ToString(), outPut.ToArray());
            return true;
        }
    }

    [Serializable]
    public class PrimaryDoctor
    {
        public string Name { get; set; }

        public int Id { get; set; }
    }

    [Serializable]
    public class DoctorsProceduresState
    {
        public string[] SelectedDoctorsIndex;

        public string[] SelectedProcedures;
    }
}