namespace Forklift
{
    public class UpdateContext
    {
        public IMetabase Metabase { get; set; }
        public ExtractionInstructions Instructions { get; set; }
    }
}