//var main = (from m in ds.LaborDtl where m.Company == Session.CompanyID && m.EndActivity == false && (m.RowMod == "A" || m.RowMod == "U") select m).FirstOrDefault();
var main = ds.LaborDtl
    .Where(m => m.Company == Session.CompanyID
        && !m.EndActivity
        && (m.RowMod == "A" || m.RowMod == "U"))
    .FirstOrDefault();

//**************************************
if (main != null)
{
    bool isFai = Db.JobHead.Any(h =>
        h.Company == main.Company
        && h.JobNum == main.JobNum
        && h.FAI_Reqd_c == true);

    if (isFai)
    {
        return;
    }

    bool hasEarlierOp = Db.JobOper.Any(o =>
        o.Company == main.Company
        && o.JobNum == main.JobNum
        && !o.OpComplete
        && (o.AssemblySeq > main.AssemblySeq
        || (o.AssemblySeq == main.AssemblySeq
            && o.OprSeq < main.OprSeq)));

    if (hasEarlierOp)
    {
        bool hasWip = Db.PartWip.Any(w =>
            w.Company == main.Company
            && w.JobNum == main.JobNum
            && w.AssemblySeq == main.AssemblySeq
            && w.OprSeq == main.OprSeq
            && w.Quantity > 0);

        if (!hasWip)
        {
            throw new Ice.BLException($"No WIP available for job {main.JobNum}, Operation {main.OprSeq}. Please locate the WIP and contact your foreman.");
        }
    }
}
