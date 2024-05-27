using Microsoft.Extensions.Options;
using SPTrans.StatusCartaoPersonalizado.Selenium.Models;
using System;
using System.Threading.Tasks;
using TwoCaptcha.Captcha;

namespace SPTrans.StatusCartaoPersonalizado.Domain.Services
{
    public interface ITwoCaptchaService
    {
        Task<string> SolveReCaptchaV2(string siteKey, string url);
    }

    public class TwoCaptchaService : ITwoCaptchaService
    {
        private readonly SeleniumSettings _seleniumSettings;
        private readonly TwoCaptcha.TwoCaptcha _solver;

        public TwoCaptchaService(IOptions<SeleniumSettings> seleniumSettings)
        {
            _seleniumSettings = seleniumSettings.Value ?? throw new ArgumentException(nameof(seleniumSettings));
            _solver = new TwoCaptcha.TwoCaptcha(_seleniumSettings.TwoCaptcha.Token);
        }

        public async Task<string> SolveReCaptchaV2(string siteKey, string url)
        {
            ReCaptcha captcha = new ReCaptcha();
            captcha.SetSiteKey(siteKey);
            captcha.SetUrl(url);

            await _solver.Solve(captcha);

            return captcha.Code;
        }
    }
}
