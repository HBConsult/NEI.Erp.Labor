var laborDtlRow = ds.LaborDtl
    .Where(m => m.Company == Session.CompanyID
        //&& !m.EndActivity
        && (m.RowMod == "A" || m.RowMod == "U"))
    .FirstOrDefault();

if (laborDtlRow != null)
{
    Decimal wipQtyAvailable = Db.PartWip
        .Where(w =>
            w.Company == laborDtlRow.Company
            && w.JobNum == laborDtlRow.JobNum
            && w.AssemblySeq == laborDtlRow.AssemblySeq
            && w.OprSeq == laborDtlRow.OprSeq
            && w.Quantity > 0)
        .Sum(w => (decimal?)w.Quantity) ?? 0m;

    if (laborDtlRow.EndActivity)
    {
        // methods for if it's an end activity operation

        // added for feature testing
        bool isFinalOp = laborDtlRow.AssemblySeq == 0
            && !Db.JobOper.Any(f =>
                f.Company == laborDtlRow.Company
                && f.JobNum == laborDtlRow.JobNum
                && f.AssemblySeq == 0
                && f.OprSeq > laborDtlRow.OprSeq);

        if (isFinalOp)
        {
            //check for JRTI conditions
            bool openDmr = Db.DMRHead.Any(d =>
                d.Company == laborDtlRow.Company 
                && d.JobNum == laborDtlRow.JobNum
                && (d.OpenDMR 
                    || (d.TotRejectedQty + d.TotAcceptedQty < d.TotDiscrepantQty)));

            if (openDmr && (((decimal?)laborDtlRow.LaborQty ?? 0m) > 0m || laborDtlRow.OpComplete))
            {
                throw new Ice.BLException("Final operation cannot be completed or any quantity logged complete when an open and/or un-dispositioned DMR exists on the job."
                    + Environment.NewLine
                    + "Erp.Labor.BO.Update.PreProc.LaborDtlUpdate"
                    + Environment.NewLine
                    + "openDMR");
            }

            bool pendingInspection = Db.NonConf.Any(n =>
                n.Company == laborDtlRow.Company
                && n.JobNum == laborDtlRow.JobNum
                && n.InspectionPending == true);

            if (pendingInspection && (((decimal?)laborDtlRow.LaborQty ?? 0m) > 0m || laborDtlRow.OpComplete))
            {
                throw new Ice.BLException("Final operation cannot be completed or any quantity logged complete when a Nonconformance inspection is still pending."
                    + Environment.NewLine
                    + "Erp.Labor.BO.Update.PreProc.LaborDtlUpdate"
                    + Environment.NewLine
                    + "pendingInspection");
            }

            bool openOps = Db.JobOper.Any(p =>
                p.Company == laborDtlRow.Company
                && p.JobNum == laborDtlRow.JobNum
                && !p.OpComplete
                && ((p.AssemblySeq == laborDtlRow.AssemblySeq && p.OprSeq < laborDtlRow.OprSeq) 
                    || p.AssemblySeq != laborDtlRow.AssemblySeq));

            if (openOps && (((decimal?)laborDtlRow.LaborQty ?? 0m) > 0m || laborDtlRow.OpComplete))
            {
                throw new Ice.BLException("Final operation cannot be completed or any quantity logged complete when an incomplete previous operation exists."
                    + Environment.NewLine
                    + "Erp.Labor.BO.Update.PreProc.LaborDtlUpdate"
                    + Environment.NewLine
                    + "openOps");
            }

            bool openMtl = Db.JobMtl.Any(t =>
                t.Company == laborDtlRow.Company 
                && t.JobNum == laborDtlRow.JobNum
                && (!t.IssuedComplete || t.IssuedQty < t.RequiredQty));

            if (openMtl && (((decimal?)laborDtlRow.LaborQty ?? 0m) > 0m || laborDtlRow.OpComplete))
            {
                throw new Ice.BLException("Final operation cannot be completed or any quantity logged complete when a material sequence is incomplete or un-issued."
                    + Environment.NewLine
                    + "Erp.Labor.BO.Update.PreProc.LaborDtlUpdate"
                    + Environment.NewLine
                    + "openMtl");
            }

            bool openLaborTrans = Db.LaborDtl.Any(l =>
                l.Company == laborDtlRow.Company
                && l.JobNum == laborDtlRow.JobNum
                && (l.ActiveTrans || l.TimeStatus == "E"));

            if (openLaborTrans && (((decimal?)laborDtlRow.LaborQty ?? 0m) > 0m || laborDtlRow.OpComplete))
            {
                throw new Ice.BLException("Final operation cannot be completed or any quantity logged complete when an active or unapproved labor transaction is pending."
                    + Environment.NewLine
                    + "Erp.Labor.BO.Update.PreProc.LaborDtlUpdate"
                    + Environment.NewLine
                    + "openLaborTrans");
            }
        }
        //end add
        if (laborDtlRow.DiscrepQty > 0 && (string.IsNullOrWhiteSpace(laborDtlRow.LaborNote) || laborDtlRow.LaborNote == "Feature: " + Environment.NewLine + "Drawing: " + Environment.NewLine + "Location: " + Environment.NewLine + "Actual Measurement / Condition: "))
        {
            throw new Ice.BLException("If logging a non-conformance, you must enter notes detailing the issue(s) with the part(s)."
                + Environment.NewLine
                + "Erp.Labor.BO.Update.PreProc.LaborDtlUpdate"
                + Environment.NewLine
                + "discrepQty > 0");
        }

        if (laborDtlRow.JCDept == "QC")
        {
            bool hasInspection = Db.NonConf.Any(n =>
                n.Company == laborDtlRow.Company
                && n.JobNum == laborDtlRow.JobNum
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
            ((decimal?)laborDtlRow.LaborQty ?? 0m)
                + ((decimal?)laborDtlRow.DiscrepQty ?? 0m)
                + ((decimal?)laborDtlRow.ScrapQty ?? 0m) 
                + ((decimal?)laborDtlRow.BatchQty ?? 0m);

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
            h.Company == laborDtlRow.Company
            && h.JobNum == laborDtlRow.JobNum
            & h.FAI_Reqd_c == true);

        bool hasEarlierOp = Db.JobOper.Any(o =>
            o.Company == laborDtlRow.Company
            && o.JobNum == laborDtlRow.JobNum
            && !o.OpComplete
            && (o.AssemblySeq > laborDtlRow.AssemblySeq
                || (o.AssemblySeq == laborDtlRow.AssemblySeq
                && o.OprSeq < laborDtlRow.OprSeq)));

        if (wipQtyAvailable <= 0m && hasEarlierOp && !isFai)
        {
            throw new Ice.BLException($"No wip available for Job Number: {laborDtlRow.JobNum}, Operation: {laborDtlRow.OprSeq}."
                + Environment.NewLine 
                + "Please locate the wip and contact your supervisor or foreman."
                + Environment.NewLine 
                + "Erp.Labor.BO.Update.PreProc.LaborDtlUpdate"
                + Environment.NewLine
                + "wipQtyAvailable <= 0m && hasEarlierOp && !isFai");
        }
    }
}