var logRow = ds.LaborDtl
    .Where(m => m.Company == Session.CompanyID
        //&& !m.EndActivity
        && (m.RowMod == "A" || m.RowMod == "U"))
    .FirstOrDefault();

if  logRow != null)
{
    Decimal wipQtyAvailable = Db.PartWip
        .Where(w =>
            w.Company == logRow.Company
            && w.JobNum == logRow.JobNum
            && w.AssemblySeq == logRow.AssemblySeq
            && w.OprSeq == logRow.OprSeq
            && w.Quantity > 0)
        .Sum(w => (decimal?)w.Quantity) ?? 0m;

    if  logRow.EndActivity)
    {
        // methods for if it's an end activity operation
        if  logRow.DiscrepQty > 0 && (string.IsNullOrWhiteSpace logRow.LaborNote) || logRow.LaborNote == "Feature: " + Environment.NewLine + "Drawing: " + Environment.NewLine + "Location: " + Environment.NewLine + "Actual Measurement / Condition: "))
        {
            throw new Ice.BLException("If logging a non-conformance, you must enter notes detailing the issue(s) with the part(s)."
                + Environment.NewLine
                + "Erp.Labor.BO.Update.PreProc.LaborDtlUpdate"
                + Environment.NewLine
                + "discrepQty > 0");
        }

        if  logRow.JCDept == "QC")
        {
            bool hasInspection = Db.NonConf.Any(n =>
                n.Company == logRow.Company
                && n.JobNum == logRow.JobNum
                && n.InspectionPending == true);

            if (hasInspection)
            {
                throw new Ice.BLException("This job has a pending inspection requirement."
                    + Environment.NewLine
                    + "QC Department operations cannot be completed until all pending inspections are complete."
                    + Environment.NewLine
                    + "Erp.Labor.Update.PreProc.LaborDtlUpdate"
                    + Environment.NewLine
                    + "hasInspection");
            }
        }

        if (wipQtyAvailable <= 0m)
        {
            throw new Ice.BLException("No job wip available to complete any quantity on this operation. Please locate job wip." 
                + Environment.NewLine 
                + "Erp.Labor.BO.Update.PreProc.LaborDtlUpdate" 
                + Environment.NewLine 
                + "!wipAvailable");
        }

        Decimal endActivityQty = 
            ((decimal? logRow.LaborQty ?? 0m)
                + ((decimal? logRow.DiscrepQty ?? 0m)
                + ((decimal? logRow.ScrapQty ?? 0m) 
                + ((decimal? logRow.BatchQty ?? 0m);

        if (endActivityQty > wipQtyAvailable)
        {
            throw new Ice.BLException("The quantity you are logging is greater than the wip quantity available on this operation." 
            + Environment.NewLine
            + "Please see your supervisor."
            + Environment.NewLine
            + "Erp.Labor.BO.Update.PreProc.LaborDtlUpdate"
            + Environment.NewLine
            + "endActivityQty > wipQty");
        }
    }
    else
    {
        // methods for it it's a start activity
        bool isFai = Db.JobHead.Any(h =>
            h.Company == logRow.Company
            && h.JobNum == logRow.JobNum
            & h.FAI_Reqd_c == true);

        bool hasEarlierOp = Db.JobOper.Any(o =>
            o.Company == logRow.Company
            && o.JobNum == logRow.JobNum
            && !o.OpComplete
            && (o.AssemblySeq > logRow.AssemblySeq
                || (o.AssemblySeq == logRow.AssemblySeq
                && o.OprSeq < logRow.OprSeq)));

        if (wipQtyAvailable <= 0m && hasEarlierOp && !isFai)
        {
            throw new Ice.BLException($"No wip available for Job Number:  logRow.JobNum}, Operation:  logRow.OprSeq}."
                + Environment.NewLine 
                + "Please locate the wip and contact your supervisor or foreman."
                + Environment.NewLine 
                + "Erp.Labor.BO.Update.PreProc.LaborDtlUpdate"
                + Environment.NewLine
                + "wipQtyAvailable <= 0m && hasEarlierOp && !isFai")
        }
    }
}