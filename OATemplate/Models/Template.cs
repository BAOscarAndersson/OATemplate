using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OATemplate.Models
{
    public class Template
    {
        public string Publication;

        Press KBA;

        internal class Press
        {
            Tower T11;
            Tower T12;
            Tower T13;
            Tower T14;

            internal class Tower
            {
                Cylinder Cyl1;
                Cylinder Cyl2;
                Cylinder Cyl3;
                Cylinder Cyl4;
                Cylinder Cyl5;
                Cylinder Cyl6;
                Cylinder Cyl7;
                Cylinder Cyl8;

                internal class Cylinder
                {
                    HighOrLow H;
                    HighOrLow L;

                    internal class HighOrLow
                    {
                        Section A;
                        Section B;
                        Section C;
                        Section D;

                        internal class Section
                        {
                            string pageNumber;
                        }
                    }
                }
            }
        }
    }
}