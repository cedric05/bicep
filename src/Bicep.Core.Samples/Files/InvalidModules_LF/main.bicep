﻿module moduleWithMissingPath './nonExistent.bicep' = {

}

module moduleWithoutPath = {

}

var interp = 'hello'
module moduleWithInterpPath './${interp}.bicep' = {

}

module moduleWithSelfCycle './main.bicep' = {

}

module './main.bicep' = {

}