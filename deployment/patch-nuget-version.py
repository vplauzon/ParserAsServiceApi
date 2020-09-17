#!/usr/bin/python3

import sys
import os
import xml.dom.minidom as md 

def writeXml(document, path):
    with open(path, "w" ) as fs:  
        fs.write(document.toxml() ) 

if len(sys.argv) != 4:
    print("There are %d arguments" % (len(sys.argv)-1))
    print("Arguments should be")
    print("1-File path")
    print("2-Patch number")
    print("3-Alpha (true or false)")
else:
    path = sys.argv[1]
    patchNumber = sys.argv[2]
    isAlpha = sys.argv[3]=='true'

    nugetPath = os.path.dirname(path)+"/nuget.csproj"
    if isAlpha:
        suffix = '-alpha'
    else:
        suffix = ''

    print ('Text file:  %s' % (path))
    print ('Patch Number:  %s' % (patchNumber))
    print ('Nuget path:  %s' % (nugetPath))

    #   Load XML and print out
    document = md.parse(path)
    print('XML content:')
    print(document.toxml())

    #   Set GeneratePackageOnBuild to true
    generate=document.getElementsByTagName('GeneratePackageOnBuild')[0]
    generate.firstChild.nodeValue = "true"

    #   Add patch to the version
    version=document.getElementsByTagName('Version')[0]
    version.firstChild.nodeValue += "." + patchNumber + suffix

    #   Output project
    print('Nuget project content:')
    print(document.toxml())
    writeXml(document, nugetPath)

