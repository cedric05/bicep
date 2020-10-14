module moduleWithMissingPath './nonExistent.bicep' = {
//@[29:50) [BCP089 (Error)] An error occurred loading the module. Received failure "Failed to find file /./nonExistent.bicep". |'./nonExistent.bicep'|

}

module moduleWithoutPath = {
//@[25:26) [BCP095 (Error)] Expected a module path string. |=|
//@[25:28) [BCP088 (Error)] Unable to find file path for module. |= {|
//@[28:28) [BCP018 (Error)] Expected the "=" character at this location. ||

}
//@[0:1) [BCP007 (Error)] This declaration type is not recognized. Specify a parameter, variable, resource, or output declaration. |}|

var interp = 'hello'
module moduleWithInterpPath './${interp}.bicep' = {
//@[28:47) [BCP090 (Error)] String interpolation is unsupported for specifying the module path. |'./${interp}.bicep'|

}

module moduleWithSelfCycle './main.bicep' = {
//@[27:41) [BCP089 (Error)] An error occurred loading the module. Received failure "Failed to find file /./main.bicep". |'./main.bicep'|

}

module './main.bicep' = {
//@[7:21) [BCP094 (Error)] Expected a module identifier at this location. |'./main.bicep'|
//@[7:21) [BCP096 (Error)] Failed to load module. |'./main.bicep'|

}
