var row = (from d in ds.LaborDtl where d.RowMod == "A" || d.RowMod == "U" select d).FirstOrDefault();
if (row != null)
{
  if (row.DiscrepQty > 0 && (row.LaborNote == "" || row.LaborNote == "Feature: " + Environment.NewLine + "Drawing: " + Environment.NewLine + "Location: " + Environment.NewLine + "Actual Measurement / Condition: "))
  {
    throw new Ice.BLException("If logging a non-conformance, you must enter notes detailing the issue(s) with the part(s).");
  }
}


var row = (from d in ds.LaborDtl where d.RowMod == "A" || d.RowMod == "U" select d).FirstOrDefault();
if (row != null && row.EndActivity == true && row.JCDept == "QC")
{
  var ins = (from i in Db.NonConf where i.Company == Session.CompanyID && i.JobNum == row.JobNum && i.InspectionPending == true select i).FirstOrDefault();
  if (ins != null)
  {
    throw new Ice.BLException("This job has a pending inspection requirement. QC Department operations cannot be completed until all pending inspections are complete.");
  }
}
//test end line