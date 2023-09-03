using ServiceLayer.Interface;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceLayer.Code
{
    public class EvaluationPostfixExpression : IEvaluationPostfixExpression
    {
        public int evaluatePostfix(string exp)
        {
            // create a stack 
            Stack<int> stack = new Stack<int>();

            // Scan all characters one by one 
            for (int i = 0; i < exp.Length; i++)
            {
                char c = exp[i];

                if (c == ' ')
                {
                    continue;
                }

                // If the scanned character is an  
                // operand (number here),extract 
                // the number. Push it to the stack. 
                else if (char.IsDigit(c) || c == '.')
                {
                    int n = 0;

                    // extract the characters and 
                    // store it in num 
                    if (char.IsDigit(c) && c != '0')
                    {
                        while (char.IsDigit(c))
                        {
                            n = n * 10 + (int)(c - '0');
                            i++;
                            c = exp[i];
                        }
                        i--;
                        // push the number in stack 
                        stack.Push(n);
                    }
                    if (c == '.')
                    {
                        i++;
                        continue;
                    }
                }

                // If the scanned character is 
                // an operator, pop two elements 
                // from stack apply the operator 
                else
                {


                    int val1 = stack.Pop();
                    int val2 = stack.Pop();

                    switch (c)
                    {
                        case '+':
                            stack.Push(val2 + val1);
                            break;

                        case '-':
                            stack.Push(val2 - val1);
                            break;

                        case '/':
                            stack.Push(val2 / val1);
                            break;

                        case '*':
                            stack.Push(val2 * val1);
                            break;
                        case '%':
                            stack.Push((val2 * val1) / 100);
                            break;
                    }
                }
            }

            if (stack.Count > 0)
                return stack.Pop();
            else
                return 0;
        }
    }
}
