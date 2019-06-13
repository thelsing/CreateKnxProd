# CreateKnxProd [![Build Status](https://dev.azure.com/thesing/CreateKnxProd/_apis/build/status/thelsing.CreateKnxProd?branchName=master)](https://dev.azure.com/thesing/CreateKnxProd/_build/latest?definitionId=1&branchName=master)

Simple Gui to create simple knxprod file for ETS. 

Configuration:
Change the path in CreateKnxProd.exe.config to your ETS4 Path. If you have ETS5 use the path to the ETS4 dlls of ETS5. For example:
C:\Program Files (x86)\ETS5\CV\4.0.1997.50261
You can also change UICulture and Culture from 'de' to 'en' there.

If ETS doesn't import the generated file, try removing underscores from product name, hardware name etc. Maybe version numbers have to be at least 10 too. 
