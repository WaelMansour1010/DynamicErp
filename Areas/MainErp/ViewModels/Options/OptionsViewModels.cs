using System.Collections.Generic;
using System.Linq;

namespace MyERP.Areas.MainErp.ViewModels.Options
{
    public class OptionsIndexViewModel
    {
        public OptionsIndexViewModel()
        {
            Categories = new List<OptionCategoryViewModel>();
            Summary = new OptionsSummaryViewModel();
            Permissions = new OptionsPermissionsViewModel();
        }

        public OptionsSummaryViewModel Summary { get; set; }
        public IList<OptionCategoryViewModel> Categories { get; set; }
        public OptionsPermissionsViewModel Permissions { get; set; }

        public int EditableFieldsCount
        {
            get { return Categories.SelectMany(x => x.Fields).Count(x => x.IsEditable); }
        }
    }

    public class OptionsSummaryViewModel
    {
        public string CompanyArabicName { get; set; }
        public string CompanyEnglishName { get; set; }
        public string VatRegistrationNumber { get; set; }
        public string Website { get; set; }
        public int FieldsCount { get; set; }
        public int BooleanFieldsCount { get; set; }
        public int EmptyFieldsCount { get; set; }
    }

    public class OptionCategoryViewModel
    {
        public OptionCategoryViewModel()
        {
            Fields = new List<OptionFieldViewModel>();
        }

        public string Key { get; set; }
        public string Title { get; set; }
        public string IconCssClass { get; set; }
        public IList<OptionFieldViewModel> Fields { get; set; }
    }

    public class OptionFieldViewModel
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string DataType { get; set; }
        public int? MaxLength { get; set; }
        public bool IsNullable { get; set; }
        public int Ordinal { get; set; }
        public string CategoryKey { get; set; }
        public string Value { get; set; }
        public bool IsBoolean { get; set; }
        public bool IsNumber { get; set; }
        public bool IsDate { get; set; }
        public bool IsLongText { get; set; }
        public bool IsSensitive { get; set; }
        public bool IsEditable { get; set; }
        public string HelpText { get; set; }
        public IList<OptionChoiceViewModel> Choices { get; set; }

        public bool HasChoices
        {
            get { return Choices != null && Choices.Count > 0; }
        }
    }

    public class OptionChoiceViewModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }

    public class OptionSaveRequest
    {
        public IList<OptionFieldValueViewModel> Fields { get; set; }
    }

    public class OptionFieldValueViewModel
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class OptionsSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int UpdatedFields { get; set; }
    }

    public class OptionsPermissionsViewModel
    {
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
    }
}
