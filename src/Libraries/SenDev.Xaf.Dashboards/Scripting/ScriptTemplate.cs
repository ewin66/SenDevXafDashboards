﻿using System;
using System.Linq;
using DevExpress.Xpo;
using |namespace|;
using SenDev.Xaf.Dashboards.Scripting;


public class Script
{
    public object GetData(ScriptContext context)
    {
        return context.Query<|classname|>().Take(1000);
    }
}

