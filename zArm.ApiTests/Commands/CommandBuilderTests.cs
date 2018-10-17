using Microsoft.VisualStudio.TestTools.UnitTesting;
using zArm.Api.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.ApiTests;

namespace zArm.Api.Commands.Tests
{
    [TestClass()]
    public class CommandBuilderTests
    {
        [TestMethod()]
        public void ParseResponses_Escape()
        {
            //escaped
            var actual = CommandBuilder.ParseResponses("-1 \"bad command.\"\n");
            var expected = new Response[] { new ErrorResponse() { Message = "bad command." } };
            AssertObjects.HaveEqualProperties(expected, actual, true);

            //not escaped
            actual = CommandBuilder.ParseResponses("-1 bad_command.\n");
            expected = new Response[] { new ErrorResponse() { Message = "bad_command." } };
            AssertObjects.HaveEqualProperties(expected, actual, true);

            //not escaped
            actual = CommandBuilder.ParseResponses("-1 bad command.\n");
            expected = new Response[] { new ErrorResponse() { Message = "bad" } };
            AssertObjects.HaveEqualProperties(expected, actual, true);
        }
    }
}