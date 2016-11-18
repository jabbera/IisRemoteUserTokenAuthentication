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

        [TestInitialize]
        public void TestInit()
        {
            this.settings = new Mock<ISettings>();
            this.adInteraction = new Mock<IAdInteraction>();
            this.httpContext = new Mock<IRutaHttpContext>();

            this.httpContext.SetupGet(x => x.IsAuthenticated).Returns(true);

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
            (string header, string loginValue) login = ValueTuple.Create("1", "Mike");
            (string header, string nameValue) name = ValueTuple.Create("2", "Dane");
            (string header, string emailValue) email = ValueTuple.Create("3", "Mike@Dan.com");
            (string header, string[] groupsValue) groups = ValueTuple.Create("4", new string[] { "A", "B", "C" });
            
            this.httpContext.SetupGet(x => x.DomainUserName).Returns(login.loginValue);
            this.adInteraction.Setup(x => x.GetUserInformation(login.loginValue)).Returns(ValueTuple.Create(login.loginValue, name.nameValue, email.emailValue, groups.groupsValue));

            this.settings.SetupGet(x => x.LoginHeader).Returns(login.header);
            this.settings.SetupGet(x => x.NameHeader).Returns(name.header);
            this.settings.SetupGet(x => x.EmailHeader).Returns(email.header);
            this.settings.SetupGet(x => x.GroupsHeader).Returns(groups.header);

            this.rutaModule.AuthorizeRequest(this.httpContext.Object);

            this.httpContext.Verify(x => x.RemoveRequestHeader("Authorization"), Times.Once());
            this.httpContext.Verify(x => x.AddRequestHeader(login.header, login.loginValue), Times.Once());
            this.httpContext.Verify(x => x.AddRequestHeader(name.header, name.nameValue), Times.Once());
            this.httpContext.Verify(x => x.AddRequestHeader(email.header, email.emailValue), Times.Once());
            this.httpContext.Verify(x => x.AddRequestHeader(groups.header, string.Join(",", groups.groupsValue)), Times.Once());
        }
    }
}
