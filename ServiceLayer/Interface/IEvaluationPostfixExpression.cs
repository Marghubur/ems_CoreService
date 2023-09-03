using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceLayer.Interface
{
    public interface IEvaluationPostfixExpression
    {
        int evaluatePostfix(string exp);
    }
}
