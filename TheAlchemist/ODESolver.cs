using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TheAlchemist
{
    static class ODESolver
    {
        public delegate float[] EvalFunction(float t, float[] current);

        public static float[] RungeKutta(float[] current, float step, EvalFunction eval)
        {   
            float halfStep = step / 2;
            float sixthStep = step / 6;
            var tmp = new float[current.Length];

            float[] k1 = eval(0, current);
            // Console.WriteLine("k1: " + string.Join(';', k1));

            for (var i = 0; i < current.Length; i++)
            {
                tmp[i] = current[i] + halfStep * k1[i];                
            }
            float[] k2 = eval(0 + halfStep, tmp);
            // Console.WriteLine("k2: " + string.Join(';', k2));

            for (var i = 0; i < current.Length; i++)
            {
                tmp[i] = current[i] + halfStep * k2[i];
            }
            float[] k3 = eval(0 + halfStep, tmp);
            // Console.WriteLine("k3: " + string.Join(';', k3));

            for (var i = 0; i < current.Length; i++)
            {
                tmp[i] = current[i] + step * k3[i];
            }
            float[] k4 = eval(0 + step, tmp);
            // Console.WriteLine("k4: " + string.Join(';', k4));


            for (var i = 0; i < current.Length; i++)
            {
                tmp[i] = current[i] + sixthStep * (k1[i] + 2 * k2[i] + 2 * k3[i] + k4[i]);
            }
          
            return tmp;
        }

        public static float[] Euler(float[] current, float step, EvalFunction eval)
        {
            var deriv = eval(step, current);            
            for (var i = 0; i < current.Length; i++)
            {
                current[i] += step * deriv[i];
            }
            return current;
        }
    }
}
