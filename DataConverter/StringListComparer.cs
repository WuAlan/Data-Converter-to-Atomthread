using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataConverter
{
    class StringListComparer : IComparer<List<String>>
    {



        public int Compare(List<string> x, List<string> y)
        {
            int ix, iy;
            ix = Convert.ToInt32(x[1]);
            iy = Convert.ToInt32(y[1]);
            int result = ix - iy;
            if (result != 0)
            {
                return result;
            }
            else if (x[2] != y[2])
            {
                if (x[2] == "JA")
                {
                    return -1;
                }
                else if (y[2] == "JA")
                {
                    return 1;
                }
                else
                {
                    if (x[2] == "JR")
                    {
                        return -1;
                    }
                    else if (y[2] == "JR")
                    {
                        return 1;
                    }
                    else
                    {
                        if (x[2] == "JP")
                        {
                            return -1;
                        }
                        else if (y[2] == "JP")
                        {
                            return 1;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                }

            }
            else
            {
                return 0;
            }
        }

    }
}
