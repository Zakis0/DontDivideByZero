using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = DontDivideByZero.Test.CSharpCodeFixVerifier<
    DontDivideByZero.DontDivideByZeroAnalyzer,
    DontDivideByZero.DontDivideByZeroCodeFixProvider>;

namespace DontDivideByZero.Test {
    [TestClass]
    public class DontDivideByZeroUnitTest {
        [TestMethod]
        public async Task NoZero_noDiagnostic() {
            var test = @"
class MyProgram {
    static void Main() {
        int a = {|#0:1 / 1|};
    }
}
";
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task ExplicitDivisionOfAConstantByZero_Diagnostic() {
            var test = @"
class MyProgram {
    static void Main() {
        int a = {|#0:1 / 0|};
    }
}
";
            var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(
                test,
                expDiag.WithLocation(0),
                DiagnosticResult.CompilerError("CS0020").WithLocation(0)
            );
        }

        [TestMethod]
        public async Task ExplicitDivisionOfAFunctionByZero_Diagnostic() {
            var test = @"
class MyProgram {
    static int f() {
        return 1;
    }

    static void Main() {
        int a = {|#0:f() / 0|};
    }
}
";
            var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(
                test,
                expDiag.WithLocation(0)
            );
        }

        [TestMethod]
        public async Task DivisionByDeclaratorVariable_Diagnostic() {
            var test = @"
class MyProgram {
    static void Main() {
        int a = 0;
        int b = {|#0:1 / a|};
    }
}
";
            var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(
                test,
                expDiag.WithLocation(0)
            );
        }
        
        [TestMethod]
        public async Task DivisionByDeclaratorVariable2_Diagnostic() {
            var test = @"
class MyProgram {
    static void Main() {
        int a = 0;
        a = {|#0:1 / a|};
    }
}
";
            var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(
                test,
                expDiag.WithLocation(0)
            );
        }

        [TestMethod]
        public async Task DivisionByDeclaratorVariable2_noDiagnostic() {
            var test = @"
class MyProgram {
    static void Main() {
        int a = 0;
        a = 1;
        int b = {|#0:1 / a|};
        a = 0;
    }
}
";
            var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task DivisionByAssignmentVariable_Diagnostic() {
            var test = @"
class MyProgram {
    static void Main() {
        int a;
        a = 0;
        int b = {|#0:1 / a|};
    }
}
";
            var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(
                test,
                expDiag.WithLocation(0)
            );
        }

        [TestMethod]
        public async Task DivisionByAssignmentVariable2_noDiagnostic() {
            var test = @"
class MyProgram {
    static void Main() {
        int a;
        a = 0;
        a = 1;
        int b = {|#0:1 / a|};
        a = 0;
    }
}
";
            var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
        
        [TestMethod]
        public async Task AssignmentInFunction_noDiagnostic() {
            var test = @"
class MyProgram {
    static int a = 1;
    static void f() {
        int a = 0;
    }
    static void Main() {
        int b = {|#0:1 / a|};
    }
}
";
            var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task SimpleArithmeticSubtractionConst_Diagnostic() {
            var test = @"
class MyProgram {
    static void Main() {
        const int a = 1;
        int b = {|#0:1 / (a - a)|};
    }
}
";
            var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(
                test,
                expDiag.WithLocation(0),
                DiagnosticResult.CompilerError("CS0020").WithLocation(0)
            );
        }

        [TestMethod]
        public async Task SimpleArithmeticMultiplicationConst_Diagnostic() {
            var test = @"
class MyProgram {
    static void Main() {
        const int a = 1;
        int b = {|#0:1 / (0 * a)|};
    }
}
";
            var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
            await VerifyCS.VerifyAnalyzerAsync(
                test,
                expDiag.WithLocation(0),
                DiagnosticResult.CompilerError("CS0020").WithLocation(0)
            );
        }
        
//         [TestMethod]
//         public async Task DivisionByVariable2_Diagnostic() {
//             var test = @"
// class MyProgram {
//     static void Main() {
//         int x = 0;
//         int x = {|#0:1 / x|};
//     }
// }
// ";
//             var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
//             await VerifyCS.VerifyAnalyzerAsync(
//                 test,
//                 expDiag.WithLocation(0)
//             );
//         }

//         [TestMethod]
//         public async Task DivisionByVariableConst_Diagnostic() {
//             var test = @"
// class MyProgram {
//     static void Main() {
//         const int a = 0;
//         int b = {|#0:1 / a|};
//     }
// }
// ";
//             var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
//             await VerifyCS.VerifyAnalyzerAsync(
//                 test,
//                 expDiag.WithLocation(0),
//                 DiagnosticResult.CompilerError("CS0020").WithLocation(0)
//             );
//         }
//
//         [TestMethod]
//         public async Task SimpleArithmeticSubtraction_Diagnostic() {
//             var test = @"
// class MyProgram {
//     static void Main() {
//         int a = 1;
//         int b = {|#0:1 / (a - a)|};
//     }
// }
// ";
//             var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
//             await VerifyCS.VerifyAnalyzerAsync(
//                 test,
//                 expDiag.WithLocation(0)
//             );
//         }
//
//         [TestMethod]
//         public async Task SimpleArithmeticMultiplication_Diagnostic() {
//             var test = @"
// class MyProgram {
//     static void Main() {
//         int a = 1;
//         int b = {|#0:1 / (0 * a)|};
//     }
// }
// ";
//             var expDiag = new DiagnosticResult(DontDivideByZeroAnalyzer.DiagnosticId, DiagnosticSeverity.Error);
//             await VerifyCS.VerifyAnalyzerAsync(
//                 test,
//                 expDiag.WithLocation(0)
//             );
//         }
    }
}
