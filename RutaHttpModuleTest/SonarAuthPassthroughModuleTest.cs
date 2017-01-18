namespace RutaHttpModuleTest
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using RutaHttpModule;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Security.Principal;
    using System.Text;
    using System.Threading.Tasks;


    [TestClass]
    public class SonarAuthPassthroughModuleTest
    {
        private SonarAuthPassthroughModule sonarAuthPassthroughModule;
        private Mock<ISettings> settings;
        private Mock<ITraceSource> traceSource;
        private Mock<ISonarAuthPassthroughHttpContext> httpContext;

        [TestInitialize]
        public void TestInit()
        {
            this.settings = new Mock<ISettings>();
            this.traceSource = new Mock<ITraceSource>();
            this.httpContext = new Mock<ISonarAuthPassthroughHttpContext>();
            
            this.sonarAuthPassthroughModule = new SonarAuthPassthroughModule(this.settings.Object, this.traceSource.Object);
        }

        [TestMethod]
        public void UserAlreadySetTest()
        {            
            this.httpContext.SetupGet(x => x.User).Returns(ClaimsPrincipal.Current);
            this.sonarAuthPassthroughModule.HandleAuthenticateRequest(httpContext.Object);

            this.httpContext.VerifySet(x => x.SkipAuthorization = true, Times.Never);
        }

        [TestMethod]
        public void SetUserWhenTokenPresentTest()
        {
            this.httpContext.SetupProperty(x => x.User);
            this.httpContext.SetupGet(x => x.HasTokenHeader).Returns(true);

            this.sonarAuthPassthroughModule.HandleAuthenticateRequest(httpContext.Object);

            this.httpContext.VerifySet(x => x.SkipAuthorization = true, Times.Once());
            this.httpContext.VerifySet(x => x.User = It.IsAny<IPrincipal>(), Times.Once());
            Assert.IsTrue(this.httpContext.Object.User.Identity.IsAuthenticated);
        }

        [TestMethod]
        public void SetWhenUserAgentMatchesTest()
        {
            this.httpContext.SetupProperty(x => x.User);
            string agentName = "MATCHME";

            this.httpContext.SetupGet(x => x.UserAgent).Returns($"{agentName}_BLAH");
            this.settings.SetupGet(x => x.PassThruUserAgents).Returns(new[] { agentName });

            this.sonarAuthPassthroughModule.HandleAuthenticateRequest(httpContext.Object);

            this.httpContext.VerifySet(x => x.SkipAuthorization = true, Times.Once());
            Assert.IsTrue(this.httpContext.Object.User.Identity.IsAuthenticated);
        }

        [TestMethod]
        public void SetWhenUserAgentOnWhitespaceTest()
        {
            this.httpContext.SetupProperty(x => x.User);
            string agentName = string.Empty;

            this.httpContext.SetupGet(x => x.UserAgent).Returns(agentName);
            this.settings.SetupGet(x => x.PassThruUserAgents).Returns(new string[0]);

            this.sonarAuthPassthroughModule.HandleAuthenticateRequest(httpContext.Object);

            this.httpContext.VerifySet(x => x.SkipAuthorization = true, Times.Once());
            Assert.IsTrue(this.httpContext.Object.User.Identity.IsAuthenticated);
        }

        [TestMethod]
        public void DontSetWhenUserAgentMatchesTest()
        {
            string agentName = "MATCHME";

            this.httpContext.SetupGet(x => x.UserAgent).Returns($"DONTMATCH_{agentName}_BLAH");
            this.settings.SetupGet(x => x.PassThruUserAgents).Returns(new[] { agentName });

            this.sonarAuthPassthroughModule.HandleAuthenticateRequest(httpContext.Object);

            this.httpContext.VerifySet(x => x.SkipAuthorization = true, Times.Never());
        }
    }
}
