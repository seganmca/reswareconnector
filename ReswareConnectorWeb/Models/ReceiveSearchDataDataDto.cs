namespace ReswareConnectorWeb.Models
{
    #region Main Request Data Model

    public class ReceiveSearchDataDataDto
    {
        public decimal? AssessedImprovementValue { get; set; }
        public decimal? AssessedLandValue { get; set; }
        public ReceiveSearchDataChainOfTitleDto[] ChainOfTitle { get; set; }
        public DateTime? CommitmentEffectiveDate { get; set; }
        public int? CommitmentInterestID { get; set; }
        public ReceiveSearchDataEasementDto[] Easements { get; set; }
        public string FileNumber { get; set; }
        public string Leasehold { get; set; }
        public string Legal { get; set; }
        public ReceiveSearchDataLienDto[] Liens { get; set; }
        public string ParcelID { get; set; }
        public string ProposedInsured { get; set; }
        public ReceiveSearchDataRequirementDto[] Requirements { get; set; }
        public ReceiveSearchDataRestrictionDto[] Restrictions { get; set; }
        public int ServiceVersion { get; set; }
        public ReceiveSearchDataTaxDto[] Taxes { get; set; }
        public string Vesting { get; set; }
        public int? YearAcquired { get; set; }
    }

    #endregion

    #region Chain of Title Models

    public class ReceiveSearchDataChainOfTitleDto
    {
        public ReceiveSearchDataBookPageDto[] BookPages { get; set; }
        public BookPageDataRowDto[] ChainOfTitleBookPages { get; set; }
        public int? ChainOfTitleID { get; set; }
        public decimal? Consideration { get; set; }
        public DateTime? Dated { get; set; }
        public string DeedBookVolumePage { get; set; }
        public string DeedType { get; set; }
        public string Grantees { get; set; }
        public string Grantors { get; set; }
        public string Instrument { get; set; }
        public string Notes { get; set; }
        public DateTime? Recorded { get; set; }
    }

    #endregion

    #region Base Models

    public class ReceiveSearchDataLienRequirementBaseDto
    {
        public int DocumentTypeID { get; set; }
        public string Against { get; set; }
        public decimal? Amount { get; set; }
        public string Assignee { get; set; }
        public string AssigneeBook { get; set; }
        public string AssigneeInstrument { get; set; }
        public string AssigneeLiber { get; set; }
        public string AssigneePage { get; set; }
        public string AssigneeVolume { get; set; }
        public string Assignor { get; set; }
        public string Book { get; set; }
        public ReceiveSearchDataBookPageDto[] BookPages { get; set; }
        public string CaseNumber { get; set; }
        public string County { get; set; }
        public string CourtDistrict { get; set; }
        public string CourtType { get; set; }
        public DateTime? Date { get; set; }
        public string DocumentName { get; set; }
        public string Endorsements { get; set; }
        public bool? Flagged { get; set; }
        public string Grantee { get; set; }
        public string Grantor { get; set; }
        public string Holder { get; set; }
        public string InFavorOf { get; set; }
        public decimal? InstallmentAmount { get; set; }
        public string InstallmentNumber { get; set; }
        public string Instrument { get; set; }
        public bool? IsAllCaps { get; set; }
        public string Language { get; set; }
        public string Liber { get; set; }
        public DateTime? MaturityDate { get; set; }
        public string Page { get; set; }
        public string Purpose { get; set; }
        public DateTime? RecordedDate { get; set; }
        public string State { get; set; }
        public string StateDistrict { get; set; }
        public string TaxYears { get; set; }
        public string Trustee { get; set; }
        public string Volume { get; set; }
    }

    public class ReceiveSearchDataEasementRestrictionBaseDto
    {
        public int DocumentTypeID { get; set; }
        public string Book { get; set; }
        public ReceiveSearchDataBookPageDto[] BookPages { get; set; }
        public DateTime? Date { get; set; }
        public string DocumentName { get; set; }
        public string Grantee { get; set; }
        public string Grantor { get; set; }
        public string Instrument { get; set; }
        public bool? IsAllCaps { get; set; }
        public string Language { get; set; }
        public string Liber { get; set; }
        public string Page { get; set; }
        public string Purpose { get; set; }
        public DateTime? RecordedDate { get; set; }
        public string Volume { get; set; }
    }

    #endregion

    #region Specific Models

    public class ReceiveSearchDataEasementDto : ReceiveSearchDataEasementRestrictionBaseDto
    {
        public BookPageDataRowDto[] EasementBookPages { get; set; }
        public int? EasementID { get; set; }
        public int? EasementTypeID { get; set; }
        public string EasementTypeName { get; set; }
        public string CustomFieldName { get; set; }

    }

    public class ReceiveSearchDataLienDto : ReceiveSearchDataLienRequirementBaseDto
    {
        public BookPageDataRowDto[] LienBookPages { get; set; }
        public int? LienID { get; set; }
        public int? LienTypeID { get; set; }
        public string LienTypeName { get; set; }
        public string CustomFieldName { get; set; }
    }

    public class ReceiveSearchDataRequirementDto : ReceiveSearchDataLienRequirementBaseDto
    {
        public BookPageDataRowDto[] LienBookPages { get; set; }
        public int? RequirementID { get; set; }
        public int? RequirementTypeID { get; set; }
        public string RequirementTypeName { get; set; }
    }

    public class ReceiveSearchDataRestrictionDto : ReceiveSearchDataEasementRestrictionBaseDto
    {
        public BookPageDataRowDto[] EasementBookPages { get; set; }
        public int? RestrictionID { get; set; }
        public int? RestrictionTypeID { get; set; }
        public string RestrictionTypeName { get; set; }
    }

    public class ReceiveSearchDataTaxDto
    {
        public bool? Estimated { get; set; }
        public decimal? ExemptionAdditional { get; set; }
        public decimal? ExemptionHomeowners { get; set; }
        public decimal? ExemptionHomesteadSupplemental { get; set; }
        public decimal? ExemptionMortgage { get; set; }
        public bool? FirstDelinquent { get; set; }
        public DateTime? FirstDelinquentDate { get; set; }
        public DateTime? FirstDiscountDate { get; set; }
        public bool? FirstDue { get; set; }
        public DateTime? FirstDueDate { get; set; }
        public bool? FirstEstimated { get; set; }
        public DateTime? FirstGoodThroughDate { get; set; }
        public decimal? FirstInstallment { get; set; }
        public bool? FirstPaid { get; set; }
        public DateTime? FirstPaidDate { get; set; }
        public bool? FirstPartiallyPaid { get; set; }
        public decimal? FirstPartiallyPaidAmount { get; set; }
        public DateTime? FirstTaxesOutDate { get; set; }
        public bool? FourthDelinquent { get; set; }
        public DateTime? FourthDelinquentDate { get; set; }
        public DateTime? FourthDiscountDate { get; set; }
        public bool? FourthDue { get; set; }
        public DateTime? FourthDueDate { get; set; }
        public bool? FourthEstimated { get; set; }
        public DateTime? FourthGoodThroughDate { get; set; }
        public decimal? FourthInstallment { get; set; }
        public bool? FourthPaid { get; set; }
        public DateTime? FourthPaidDate { get; set; }
        public bool? FourthPartiallyPaid { get; set; }
        public decimal? FourthPartiallyPaidAmount { get; set; }
        public DateTime? FourthTaxesOutDate { get; set; }
        public decimal? Improvements { get; set; }
        public decimal? Land { get; set; }
        public string Notes { get; set; }
        public decimal? Other { get; set; }
        public int? PaymentFrequencyTypeID { get; set; }
        public string PaymentFrequencyTypeName { get; set; }
        public bool? SecondDelinquent { get; set; }
        public DateTime? SecondDelinquentDate { get; set; }
        public DateTime? SecondDiscountDate { get; set; }
        public bool? SecondDue { get; set; }
        public DateTime? SecondDueDate { get; set; }
        public bool? SecondEstimated { get; set; }
        public DateTime? SecondGoodThroughDate { get; set; }
        public decimal? SecondInstallment { get; set; }
        public bool? SecondPaid { get; set; }
        public DateTime? SecondPaidDate { get; set; }
        public bool? SecondPartiallyPaid { get; set; }
        public decimal? SecondPartiallyPaidAmount { get; set; }
        public DateTime? SecondTaxesOutDate { get; set; }
        public string StateIDNumber { get; set; }
        public int? TaxID { get; set; }
        public string TaxIDNumber { get; set; }
        public string TaxIDNumberFurtherDescribed { get; set; }
        public int? TaxTypeID { get; set; }
        public string TaxTypeName { get; set; }
        public string TaxingEntity { get; set; }
        public string TaxingEntityCity { get; set; }
        public string TaxingEntityPhone { get; set; }
        public string TaxingEntityState { get; set; }
        public string TaxingEntityStreet1 { get; set; }
        public string TaxingEntityStreet2 { get; set; }
        public string TaxingEntityZipCode { get; set; }
        public bool? ThirdDelinquent { get; set; }
        public DateTime? ThirdDelinquentDate { get; set; }
        public DateTime? ThirdDiscountDate { get; set; }
        public bool? ThirdDue { get; set; }
        public DateTime? ThirdDueDate { get; set; }
        public bool? ThirdEstimated { get; set; }
        public DateTime? ThirdGoodThroughDate { get; set; }
        public decimal? ThirdInstallment { get; set; }
        public bool? ThirdPaid { get; set; }
        public DateTime? ThirdPaidDate { get; set; }
        public bool? ThirdPartiallyPaid { get; set; }
        public decimal? ThirdPartiallyPaidAmount { get; set; }
        public DateTime? ThirdTaxesOutDate { get; set; }
        public decimal? TotalAnnualTax { get; set; }
        public int? Year { get; set; }
    }

    public class ReceiveSearchDataBookPageDto
    {
        public string Book { get; set; }
        public int BookPageID { get; set; }
        public int BookPageSourceTypeID { get; set; }
        public string Page { get; set; }
        public int ReferenceID { get; set; }
        public int SortOrder { get; set; }
    }

    public class BookPageDataRowDto : ReceiveSearchDataLienRequirementBaseDto
    {
        public int BookPageID { get; set; }
        public int BookPageSourceTypeID { get; set; }
        public int ReferenceID { get; set; }
        public int SortOrder { get; set; }
    }

    #endregion

}
