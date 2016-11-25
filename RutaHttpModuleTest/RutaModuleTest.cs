using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RutaHttpModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace RutaHttpModuleTest
{
    [TestClass]
    public class RutaModuleTest
    {


        private RutaModule rutaModule;
        private Mock<ISettings> settings;
        private Mock<IAdInteraction> adInteraction;
        private Mock<IRutaHttpContext> httpContext;
        private (string header, string loginValue) login;
        private (string header, string nameValue) name;
        private (string header, string emailValue) email;
        private (string header, string[] groupsValue) groups;

        [TestInitialize]
        public void TestInit()
        {
            this.settings = new Mock<ISettings>();
            this.adInteraction = new Mock<IAdInteraction>();
            this.httpContext = new Mock<IRutaHttpContext>();

            this.httpContext.SetupGet(x => x.IsAuthenticated).Returns(true);

            login = ValueTuple.Create("1", "Mike");
            name = ValueTuple.Create("2", "Dane");
            email = ValueTuple.Create("3", "Mike@Dan.com");
            groups = ValueTuple.Create("4", new string[] { "A", "B", "C" });

            this.httpContext.SetupGet(x => x.DomainUserName).Returns(login.loginValue);
            this.adInteraction.Setup(x => x.GetUserInformation(login.loginValue)).Returns(ValueTuple.Create(login.loginValue, name.nameValue, email.emailValue, groups.groupsValue));

            this.settings.SetupGet(x => x.LoginHeader).Returns(this.login.header);
            this.settings.SetupGet(x => x.NameHeader).Returns(this.name.header);
            this.settings.SetupGet(x => x.EmailHeader).Returns(this.email.header);
            this.settings.SetupGet(x => x.GroupsHeader).Returns(this.groups.header);

            this.rutaModule = new RutaModule(this.adInteraction.Object, this.settings.Object);
        }

        [TestMethod]
        public void NoAuthenticationTest()
        {
            this.httpContext.SetupGet(x => x.IsAuthenticated).Returns(false);

            this.rutaModule.AuthorizeRequest(this.httpContext.Object);

            this.httpContext.Verify(x => x.RemoveRequestHeader(It.IsAny<string>()), Times.Never());
            this.httpContext.Verify(x => x.AddRequestHeader(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void NoUsernameTest()
        {
            this.httpContext.SetupGet(x => x.DomainUserName).Returns((string)null);

            this.rutaModule.AuthorizeRequest(this.httpContext.Object);

            this.httpContext.Verify(x => x.RemoveRequestHeader(It.IsAny<string>()), Times.Never());
            this.httpContext.Verify(x => x.AddRequestHeader(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void NormalFlowTest()
        {
            this.rutaModule.AuthorizeRequest(this.httpContext.Object);

            this.httpContext.Verify(x => x.RemoveRequestHeader("Authorization"), Times.Once());
            this.httpContext.Verify(x => x.AddRequestHeader(this.login.header, login.loginValue), Times.Once());
            this.httpContext.Verify(x => x.AddRequestHeader(this.name.header, name.nameValue), Times.Once());
            this.httpContext.Verify(x => x.AddRequestHeader(this.email.header, email.emailValue), Times.Once());
            this.httpContext.Verify(x => x.AddRequestHeader(this.groups.header, string.Join(",", groups.groupsValue)), Times.Once());
        }

        [TestMethod]
        public void AppendStringUserTest()
        {
            string extraTest = "@domain";
            string expectedOutput = $"{login.loginValue}{extraTest}";
            this.settings.SetupGet(x => x.AppendString).Returns(extraTest);

            this.rutaModule.AuthorizeRequest(this.httpContext.Object);

            this.httpContext.Verify(x => x.AddRequestHeader(this.login.header, expectedOutput), Times.Once());
        }

        [TestMethod]
        public void AppendStringGroupTest()
        {
            string extraTest = "@domain";
            string expectedOutput = string.Join(",", groups.groupsValue.Select(x => $"{x}{extraTest}"));

            this.settings.SetupGet(x => x.AppendString).Returns(extraTest);

            this.rutaModule.AuthorizeRequest(this.httpContext.Object);

            this.httpContext.Verify(x => x.AddRequestHeader(this.groups.header, expectedOutput), Times.Once());
        }

        [TestMethod]
        public void AppendStringDoesntThrow()
        {
            this.settings.SetupGet(x => x.AppendString).Returns((string)null);
            this.rutaModule.AuthorizeRequest(this.httpContext.Object);
        }

        [TestMethod]
        public void DowncaseUserTest()
        {
            string expectedOutput = login.loginValue.ToLower();
            this.settings.SetupGet(x => x.DowncaseUsers).Returns(true);

            this.rutaModule.AuthorizeRequest(this.httpContext.Object);

            this.httpContext.Verify(x => x.AddRequestHeader(this.login.header, expectedOutput), Times.Once());
        }

        [TestMethod]
        public void DowncaseGroupTest()
        {         
            string expectedOutput = string.Join(",", groups.groupsValue.Select(x => x.ToLower()));
            this.settings.SetupGet(x => x.DowncaseGroups).Returns(true);

            this.rutaModule.AuthorizeRequest(this.httpContext.Object);

            this.httpContext.Verify(x => x.AddRequestHeader(this.groups.header, expectedOutput), Times.Once());
        }
    }
}
