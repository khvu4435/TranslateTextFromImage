namespace ScanTextImage.Model
{
    public class SaveModel
    {
        private int? _id;
        public int? id
        {
            get
            {
                return _id;
            }

            set
            {
                if (value < 0 || value > 9)
                {
                    throw new ArgumentException("id data save should be in the range from 1 - 9");
                }
                else
                {
                    _id = value;
                }
            }
        }
        public string nameSave { get; set; } = string.Empty;

        public SelectedRegion selectedRangeSave { get; set; } = new SelectedRegion();

        public LanguageModel languageTranslateFrom { get; set; } = new LanguageModel();
        public LanguageModel languageTranslateTo { get; set; } = new LanguageModel();

        public static SaveModel DefaultSaveData()
        {
            return new SaveModel
            {
                id = null,
                selectedRangeSave = new SelectedRegion
                {
                    scaledX = 0,
                    scaledY = 0,
                    Width = 0,
                    Height = 0,
                },
                languageTranslateFrom = new LanguageModel
                {
                    LangCode = "vie",
                    LangName = "vietnamese"
                },
                languageTranslateTo = new LanguageModel
                {
                    LangCode = "eng",
                    LangName = "english"
                }
            };
        }

        public SaveModel Clone()
        {
            return new SaveModel
            {
                id = this.id,
                nameSave = this.nameSave,
                selectedRangeSave = this.selectedRangeSave,
                languageTranslateFrom = this.languageTranslateFrom,
                languageTranslateTo = this.languageTranslateTo,

            };
        }

    }
}
