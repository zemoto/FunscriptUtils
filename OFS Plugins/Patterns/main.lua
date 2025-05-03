local vibrateModule = require("vibrate")
local changingMaxModule = require("changingMax")
local doublerModule = require("doubler")
local sharpenImpactModule = require("sharpenImpact")
local softenImpactModule = require("softenImpact")
local presetModule = require("preset")
local rampModule = require("ramp")

local Pattern = {}
Pattern.SelectedPatternIdx = 1
Pattern.SpeedLimit = 400
Pattern.MinSpeed = 40

function binding.pattern()
	applyPattern()
end

function init()
	vibrateModule.SpeedLimit = Pattern.SpeedLimit
	vibrateModule.MinSpeed = Pattern.MinSpeed
end

function update(delta)
end

function gui()
	if ofs.Button( "Apply Pattern" ) then
		applyPattern()
	end
	
	ofs.Separator()

	Pattern.SelectedPatternIdx, changed = ofs.Combo( "", Pattern.SelectedPatternIdx, { "Vibrate", "Changing Max", "Doubler", "Sharpen Impact", "Soften Impact", "Preset", "Ramp" } )
	
	if Pattern.SelectedPatternIdx == 1 then -- Vibrate
		vibrateModule.gui()
	elseif Pattern.SelectedPatternIdx == 2 then -- Changing Max
		changingMaxModule.StartMax, startChanged = ofs.SliderInt("Start Max", changingMaxModule.StartMax, 0, 100)
		
		changingMaxModule.EndMax, endChanged = ofs.SliderInt("End Max", changingMaxModule.EndMax, 0, 100)
		
		if ofs.Button( "Swap" ) then
			local oldStart = changingMaxModule.StartMax
			changingMaxModule.StartMax = changingMaxModule.EndMax
			changingMaxModule.EndMax = oldStart
		end 
	elseif Pattern.SelectedPatternIdx == 5 then -- Soften Impact
		softenImpactModule.gui()
	elseif Pattern.SelectedPatternIdx == 6 then -- Preset
		presetModule.gui()
	elseif Pattern.SelectedPatternIdx == 7 then -- Ramp
		rampModule.gui()
	end
end

function applyPattern()
	if Pattern.SelectedPatternIdx == 1 then -- Vibrate
		vibrateModule.vibrate()
	elseif Pattern.SelectedPatternIdx == 2 then -- Changing Max
		changingMaxModule.changingMax()
	elseif Pattern.SelectedPatternIdx == 3 then -- Doubler
		doublerModule.doubler()
	elseif Pattern.SelectedPatternIdx == 4 then -- Sharpen Impact
		sharpenImpactModule.sharpenImpact()
	elseif Pattern.SelectedPatternIdx == 5 then -- Soften Impact
		softenImpactModule.softenImpact(Pattern.SpeedLimit)
	elseif Pattern.SelectedPatternIdx == 6 then -- Preset
		presetModule.applySelectedPreset()
	elseif Pattern.SelectedPatternIdx == 7 then -- Ramp
		rampModule.apply()
	end
end