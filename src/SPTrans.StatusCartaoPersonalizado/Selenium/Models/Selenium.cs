namespace SPTrans.StatusCartaoPersonalizado.Selenium.Models
{
    public class SeleniumSettings
    {
        public SPTrans SPTrans { get; set; }
        public TwoCaptcha TwoCaptcha { get; set; }
    }

    public class SPTrans
    {
        public string URL { get; set; }
    }

    public class TwoCaptcha
    {
        public string Token { get; set; }
    }
}
