namespace RutaHttpModuleTest
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Web;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using RutaHttpModule;
   
    [TestClass]
    public class RutaModuleTest
    {
        private RutaModule rutaModule;
        private Mock<ISettings> settings;
        private Mock<IAdInteraction> adInteraction;
        private Mock<ITraceSource> traceSource;
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
            this.traceSource = new Mock<ITraceSource>();
            this.httpContext = new Mock<IRutaHttpContext>();
            
            login = ("1", "Mike");
            name = ("2", "Dane");
            email = ("3", "Mike@Dan.com");
            groups = ("4", new[] { "A", "B", "C" });
           
            this.rutaModule = new RutaModule(this.adInteraction.Object, this.settings.Object, this.traceSource.Object);
        }

        [TestMethod]
        public void NoAuthenticationTest()
        {
            // Arrange
            this.httpContext.SetupGet(x => x.IsAuthenticated).Returns(false);

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.RemoveRequestHeader(It.IsAny<string>()), Times.Never());
            this.httpContext.Verify(x => x.AddRequestHeader(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void NoDomainUserNameTest()
        {
            // Arrange
            this.httpContext.SetupGet(x => x.IsAuthenticated).Returns(true);
            this.httpContext.SetupGet(x => x.DomainUserName).Returns((string)null);

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.RemoveRequestHeader(It.IsAny<string>()), Times.Never());
            this.httpContext.Verify(x => x.AddRequestHeader(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void WhiteSpaceDomainUserNameTest()
        {
            // Arrange
            this.httpContext.SetupGet(x => x.IsAuthenticated).Returns(true);
            this.httpContext.SetupGet(x => x.DomainUserName).Returns(" ");

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.RemoveRequestHeader(It.IsAny<string>()), Times.Never());
            this.httpContext.Verify(x => x.AddRequestHeader(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void NoValidADUserTest()
        {
            // Arrange
            this.SetupNormalFlow();
            this.adInteraction.Setup(x => x.GetUserInformation(login.loginValue))
                              .Returns(((string)null, name.nameValue, email.emailValue, groups.groupsValue));

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.RemoveRequestHeader(It.IsAny<string>()), Times.Never());
            this.httpContext.Verify(x => x.AddRequestHeader(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void WhiteSpaceNoValidADUserTest()
        {
            // Arrange
            this.SetupNormalFlow();
            this.adInteraction.Setup(x => x.GetUserInformation(login.loginValue))
                              .Returns((" ", name.nameValue, email.emailValue, groups.groupsValue));

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.RemoveRequestHeader(It.IsAny<string>()), Times.Never());
            this.httpContext.Verify(x => x.AddRequestHeader(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [TestMethod]
        public void NormalFlowTest()
        {
            // Arrange
            this.SetupNormalFlow();

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.RemoveRequestHeader("Authorization"), Times.Once());
            this.httpContext.Verify(x => x.AddRequestHeader(this.login.header, login.loginValue), Times.Once());
            this.httpContext.Verify(x => x.AddRequestHeader(this.name.header, name.nameValue), Times.Once());
            this.httpContext.Verify(x => x.AddRequestHeader(this.email.header, email.emailValue), Times.Once());
            this.httpContext.Verify(x => x.AddRequestHeader(this.groups.header, string.Join(",", groups.groupsValue)), Times.Once());
        }

        [TestMethod]
        public void AppendStringUserTest()
        {
            // Arrange
            string extraTest = "@domain";
            string expectedOutput = $"{login.loginValue}{extraTest}";

            this.SetupNormalFlow();            
            this.settings.SetupGet(x => x.AppendString).Returns(extraTest);
           
            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.AddRequestHeader(this.login.header, expectedOutput), Times.Once());
        }

        [TestMethod]
        public void WhiteSpaceAppendStringUserTest()
        {
            // Arrange
            string extraTest = " ";
            string expectedOutput = login.loginValue;

            this.SetupNormalFlow();     
            this.settings.SetupGet(x => x.AppendString).Returns(extraTest);

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.AddRequestHeader(this.login.header, expectedOutput), Times.Once());
        }

        [TestMethod]
        public void NullAppendStringUserTest()
        {
            // Arrange
            string expectedOutput = login.loginValue;

            this.SetupNormalFlow();
            this.settings.SetupGet(x => x.AppendString).Returns((string)null);

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.AddRequestHeader(this.login.header, expectedOutput), Times.Once());
        }

        [TestMethod]
        public void AppendStringGroupTest()
        {
            // Arrange
            string extraTest = "@domain";
            string expectedOutput = string.Join(",", groups.groupsValue.Select(x => $"{x}{extraTest}"));

            this.SetupNormalFlow();       
            this.settings.SetupGet(x => x.AppendString).Returns(extraTest);

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.AddRequestHeader(this.groups.header, expectedOutput), Times.Once());
        }

        [TestMethod]
        public void WhiteSpaceAppendStringGroupTest()
        {
            // Arrange
            string extraTest = " ";
            string expectedOutput = string.Join(",", groups.groupsValue);

            this.SetupNormalFlow();          
            this.settings.SetupGet(x => x.AppendString).Returns(extraTest);

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.AddRequestHeader(this.groups.header, expectedOutput), Times.Once());
        }

        [TestMethod]
        public void NullAppendStringGroupTest()
        {
            // Arrange
            string expectedOutput = string.Join(",", groups.groupsValue);

            this.SetupNormalFlow();          
            this.settings.SetupGet(x => x.AppendString).Returns((string)null);

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.AddRequestHeader(this.groups.header, expectedOutput), Times.Once());
        }

        [TestMethod]
        public void LowercaseUserTest()
        {
            // Arrange
            string expectedOutput = login.loginValue.ToLower();

            this.SetupNormalFlow();
            this.settings.SetupGet(x => x.DowncaseUsers).Returns(true);

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.AddRequestHeader(this.login.header, expectedOutput), Times.Once());
        }

        [TestMethod]
        public void LowercaseGroupTest()
        {         
            // Arrange
            string expectedOutput = string.Join(",", groups.groupsValue.Select(x => x.ToLower()));

            this.SetupNormalFlow();            
            this.settings.SetupGet(x => x.DowncaseGroups).Returns(true);

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.AddRequestHeader(this.groups.header, expectedOutput), Times.Once());
        }

        [TestMethod]
        public void NullOrEmptyGroupTest()
        {
            // Arrange
            var testGroup = new[] { "A", null, " ", "C" };
            string expectedOutput = string.Join(",", new[] { "A", "C" });

            this.SetupNormalFlow();
            this.adInteraction.Setup(x => x.GetUserInformation(login.loginValue))
                              .Returns((login.loginValue, name.nameValue, email.emailValue, testGroup));

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.httpContext.Verify(x => x.AddRequestHeader(this.groups.header, expectedOutput), Times.Once());
        }

        [TestMethod]
        public void DisposeNoExceptionTest()
        {
            // Arrange
            var module = new RutaModule(this.adInteraction.Object, this.settings.Object, this.traceSource.Object);

            // Act
            module.Dispose();

            // Assert

            // NoExceptionThrown
        }

        [TestMethod]
        public void ModuleNameTest()
        {
            // Arrange
            const string expectedValue = "RutaModule";

            // Act
            var foundValue = this.rutaModule.ModuleName;

            // Assert
            Assert.AreEqual(expectedValue, foundValue);
        }

        [TestMethod]
        public void InitNoExceptionTest()
        {
            // Arrange
            HttpApplication app = new HttpApplication();

            // Act
            this.rutaModule.Init(app);

            // Assert

            // NoExceptionThrown
        }

        [TestMethod]
        public void NormalTraceTest()
        {
            // Arrange

            // Act
            this.rutaModule.HandleAuthorizeRequest(this.httpContext.Object);

            // Assert
            this.traceSource.Verify(x => x.TraceEvent(TraceEventType.Start, 0, "START AuthorizeRequest"), Times.Once());
            this.traceSource.Verify(x => x.TraceEvent(TraceEventType.Error, 0, It.IsAny<string>()), Times.Never());
            this.traceSource.Verify(x => x.TraceEvent(TraceEventType.Stop, 0, "END AuthorizeRequest"), Times.Once());
        }

        [TestMethod]
        public void ExceptionTraceTest()
        {
            // Arrange
            bool exceptionThrown = false;
            string exceptionMessage = null;
            this.traceSource.Setup(x => x.TraceEvent(TraceEventType.Error, 0, It.IsAny<string>()))
                            .Callback((TraceEventType type, int id, string msg) => exceptionMessage = msg);

            // Act
            try
            {
                this.rutaModule.HandleAuthorizeRequest(null);
            }
            catch (Exception)
            {
                exceptionThrown = true;
            }

            // Assert
            Assert.IsTrue(exceptionThrown);

            this.traceSource.Verify(x => x.TraceEvent(TraceEventType.Start, 0, "START AuthorizeRequest"), Times.Once());
            this.traceSource.Verify(x => x.TraceEvent(TraceEventType.Error, 0, It.IsAny<string>()), Times.Once());
            this.traceSource.Verify(x => x.TraceEvent(TraceEventType.Stop, 0, "END AuthorizeRequest"), Times.Once());

            Assert.IsTrue(exceptionMessage.StartsWith("ERROR AuthorizeRequest: ExceptionData:"));
        }

        private void SetupNormalFlow()
        {
            this.httpContext.SetupGet(x => x.IsAuthenticated).Returns(true);
            this.httpContext.SetupGet(x => x.DomainUserName).Returns(login.loginValue);

            this.adInteraction.Setup(x => x.GetUserInformation(login.loginValue))
                              .Returns((login.loginValue, name.nameValue, email.emailValue, groups.groupsValue));

            this.settings.SetupGet(x => x.LoginHeader).Returns(this.login.header);
            this.settings.SetupGet(x => x.NameHeader).Returns(this.name.header);
            this.settings.SetupGet(x => x.EmailHeader).Returns(this.email.header);
            this.settings.SetupGet(x => x.GroupsHeader).Returns(this.groups.header);
        }
    }
}
