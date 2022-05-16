public class AlgorithmFormat
{
    public string WCC { get; set; }
    public string BMI { get; set; }
    public string PLT { get; set; }

    public string MCV { get; set; }
    public string MPV { get; set; }

    public string BASA { get; set; }
    public string Age { get; set; }
    public string LYMA { get; set; }
    public string SBP { get; set; }
    public string DBP { get; set; }
    public string Ethnicity { get; set; }
    public string Proteinuria { get; set; }
    public void setObservation(string obsKey, string obsValue)
    {
        //process the observation
        //There are smarter ways than a switch to do this but for the sake of easy code I went with this.
        switch (obsKey)
            {
                case "000-0":
                    this.WCC = obsValue;
                    break;
                case "X000-1":
                    this.BMI = obsValue;
                    break;
                case "777-3":
                    this.PLT = obsValue;
                    break;
                case "787-2":
                    this.MCV = obsValue;
                    break;
                case "X000-2":
                    this.MPV = obsValue;
                    break;
                case "704-7":
                    this.BASA = obsValue;
                    break;
                case "X000-3":
                    this.LYMA = obsValue;
                    break;
                case "X000-4":
                    this.SBP = obsValue;
                    break;
                case "X000-5":
                    this.DBP = obsValue;
                    break;
                case "X000-6":
                    this.Proteinuria = obsValue;
                    break;
            }
    }
}
