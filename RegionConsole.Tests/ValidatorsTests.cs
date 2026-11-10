using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RegionConsole.Tests
{
    [TestClass]
    public class ValidatorsTests
    {
        [TestMethod]
        public void ValidateSsid_Null_ReturnsFalse()
        {
            Assert.IsFalse(Validators.ValidateSsid(null, out var error));
            Assert.IsNotNull(error);
        }

        [TestMethod]
        public void ValidateSsid_EmptyOrWhitespace_ReturnsFalse()
        {
            Assert.IsFalse(Validators.ValidateSsid("", out var error1));
            Assert.IsFalse(Validators.ValidateSsid("   ", out var error2));
        }

        [TestMethod]
        public void ValidateSsid_TooLong_ReturnsFalse()
        {
            Assert.IsFalse(Validators.ValidateSsid(new string('a', 33), out var error));
        }

        [TestMethod]
        public void ValidateSsid_ControlChars_ReturnsFalse()
        {
            Assert.IsFalse(Validators.ValidateSsid("WiFi\u0001Name", out var error));
        }

        [TestMethod]
        public void ValidateSsid_Valid_ReturnsTrue()
        {
            Assert.IsTrue(Validators.ValidateSsid("RegionConsole_AP", out var error));
            Assert.IsNull(error);
        }

        [TestMethod]
        public void ValidatePassword_Null_ReturnsFalse()
        {
            Assert.IsFalse(Validators.ValidatePassword(null, out var error));
        }

        [TestMethod]
        public void ValidatePassword_TooShort_ReturnsFalse()
        {
            Assert.IsFalse(Validators.ValidatePassword("short", out var error));
        }

        [TestMethod]
        public void ValidatePassword_TooLong_ReturnsFalse()
        {
            Assert.IsFalse(Validators.ValidatePassword(new string('a', 64), out var error));
        }

        [TestMethod]
        public void ValidatePassword_ControlChars_ReturnsFalse()
        {
            Assert.IsFalse(Validators.ValidatePassword("pass\u0001word12", out var error));
        }

        [TestMethod]
        public void ValidatePassword_Valid_ReturnsTrue()
        {
            Assert.IsTrue(Validators.ValidatePassword("Abcdef12345678", out var error));
            Assert.IsNull(error);
        }

        [TestMethod]
        public void ValidateDistinct_SameAdapters_ReturnsFalse()
        {
            Assert.IsFalse(Validators.ValidateDistinct("Wi-Fi", "wi-fi", out var error));
        }

        [TestMethod]
        public void ValidateDistinct_Empty_ReturnsFalse()
        {
            Assert.IsFalse(Validators.ValidateDistinct("", "Wi-Fi", out var error));
        }

        [TestMethod]
        public void ValidateDistinct_Different_ReturnsTrue()
        {
            Assert.IsTrue(Validators.ValidateDistinct("VPN", "Wi-Fi 2", out var error));
            Assert.IsNull(error);
        }
    }
}
