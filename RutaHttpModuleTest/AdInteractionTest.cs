using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RutaHttpModule;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Moq;

namespace RutaHttpModuleTest
{
    [TestClass]
    public class AdInteractionTest
    {
        private static Regex emailRegex = new Regex(@"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?", RegexOptions.Compiled);

        private AdInteraction adInteraction;
        private Mock<ISettings> settings;

        [TestInitialize]
        public void Init()
        {
            this.settings = new Mock<ISettings>();
            this.settings.SetupGet(x => x.AdGroupBaseDn).Returns(string.Empty);
            this.settings.SetupGet(x => x.AdUserBaseDn).Returns(string.Empty);
            this.settings.SetupGet(x => x.Downcase).Returns(true);

            this.adInteraction = new AdInteraction(this.settings.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullSettingsTest()
        {
            new AdInteraction(null);
        }

        [TestMethod]
        public void BasicInfoTest()
        {
            var result = this.adInteraction.GetUserInformation(WindowsIdentity.GetCurrent().Name);

            Assert.IsTrue(WindowsIdentity.GetCurrent().Name.EndsWith(result.login, StringComparison.OrdinalIgnoreCase));
            Assert.IsNotNull(result.name);
            Assert.IsTrue(emailRegex.IsMatch(result.email));
            CollectionAssert.Contains(result.groups, "Domain Users");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullDomainUserTest()
        {
            var result = this.adInteraction.GetUserInformation(null);
        }

        [TestMethod]
        public void NoSuchUserTest()
        {
            var result = this.adInteraction.GetUserInformation("Domain\\NoSuchUserShouldExist");
            Assert.IsNull(result.login);
        }

        [TestMethod]
        public void DefaultNoDomainTest()
        {
            var result = this.adInteraction.GetUserInformation(WindowsIdentity.GetCurrent().Name.Split('\\')[1]);
            Assert.IsTrue(WindowsIdentity.GetCurrent().Name.EndsWith(result.login, StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void GroupDnFilterTest()
        {
            this.settings.SetupGet(x => x.AdGroupBaseDn).Returns("DON'T MATCH");
            var result = this.adInteraction.GetUserInformation(WindowsIdentity.GetCurrent().Name);

            Assert.IsTrue(WindowsIdentity.GetCurrent().Name.EndsWith(result.login, StringComparison.OrdinalIgnoreCase));
            Assert.IsNotNull(result.name);
            Assert.IsTrue(emailRegex.IsMatch(result.email));
            CollectionAssert.DoesNotContain(result.groups, "Domain Users");
        }

        [TestMethod]
        public void UserDnFilterTest()
        {
            this.settings.SetupGet(x => x.AdUserBaseDn).Returns("DON'T MATCH");
            var result = this.adInteraction.GetUserInformation(WindowsIdentity.GetCurrent().Name);

            Assert.IsNull(result.login);
        }
    }
}
