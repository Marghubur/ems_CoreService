using System;
using System.Collections.Generic;
using System.Text;

namespace BottomhalfCore.Model
{
    public class ApplicationDetail
    {
        //
        // Summary:
        //     Gets or sets the name of the application. This property is automatically set
        //     by the host to the assembly containing the application entry point.
        public string ApplicationName { get; set; }
        //
        // Summary:
        //     Gets or sets the absolute path to the directory that contains the application
        //     content files.
        public string ContentRootPath { get; set; }
        //
        // Summary:
        //     Gets or sets the name of the environment. The host automatically sets this property
        //     to the value of the of the "environment" key as specified in configuration.
        public string EnvironmentName { get; set; }
    }
}
