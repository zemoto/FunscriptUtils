local vibrateModule = require("vibrate")
local changingMaxModule = require("changingMax")
local doublerModule = require("doubler")
local fillerModule = require("filler")
local sharpenImpactModule = require("sharpenImpact")
local softenImpactModule = require("softenImpact")

local Pattern = {}
Pattern.SelectedPatternIdx = 1
Pattern.SpeedLimit = 453

local Vibrate = {}
Vibrate.Intensity = 100
Vibrate.Speed = Pattern.SpeedLimit
Vibrate.TimeBetweenVibrations = 0.05

local ChangingMax = {}
ChangingMax.StartMax = 50
ChangingMax.EndMax = 50

local Filler = {}
Filler.Distance = 40
Filler.Gap = 1

local SoftenImpact = {}
SoftenImpact.PercentDistance = 15

function binding.pattern()
	applyPattern()
end

function init()
end

function update(delta)
end

function gui()
	if ofs.Button( "Apply Pattern" ) then
		applyPattern()
	end
	
	ofs.SameLine()
	
	if ofs.Button("Default") then
		if Pattern.SelectedPatternIdx == 2 then -- Changing Max
			ChangingMax.StartMax = 100
			ChangingMax.EndMax = 0
		end
	end
	
	ofs.Separator()

	Pattern.SelectedPatternIdx, changed = ofs.Combo( "", Pattern.SelectedPatternIdx, { "Vibrate", "Changing Max", "Doubler", "Filler", "Sharpen Impact", "Soften Impact" } )

	if Pattern.SelectedPatternIdx == 1 then -- Vibrate
		Vibrate.Intensity, intensityChanged = ofs.SliderInt("Intensity(%)", Vibrate.Intensity, 45, 100)
		if intensityChanged then
			Vibrate.Speed = Pattern.SpeedLimit * (Vibrate.Intensity / 100.0)
			if ofs.Undo() then
				applyPattern()
			end
		end
	elseif Pattern.SelectedPatternIdx == 2 then -- Changing Max
		ChangingMax.StartMax, startChanged = ofs.SliderInt("Start Max", ChangingMax.StartMax, 0, 100)
		
		ChangingMax.EndMax, endChanged = ofs.SliderInt("End Max", ChangingMax.EndMax, 0, 100)
	elseif Pattern.SelectedPatternIdx == 6 then -- Soften Impact
		SoftenImpact.PercentDistance, changed = ofs.SliderInt("Percent Distance", SoftenImpact.PercentDistance, 10, 40)
		if changed and ofs.Undo() then
			applyPattern()
		end
	end
end

function applyPattern()
	local script = ofs.Script(ofs.ActiveIdx())
	
	if Pattern.SelectedPatternIdx == 1 then -- Vibrate
		vibrateModule.vibrate(Vibrate.Speed,Vibrate.TimeBetweenVibrations)
	elseif Pattern.SelectedPatternIdx == 2 then -- Changing Max
		changingMaxModule.changingMax(ChangingMax.StartMax,ChangingMax.EndMax)
	elseif Pattern.SelectedPatternIdx == 3 then -- Doubler
		doublerModule.doubler()
	elseif Pattern.SelectedPatternIdx == 4 then -- Filler
		fillerModule.filler(Filler.Distance,Filler.Gap)
	elseif Pattern.SelectedPatternIdx == 5 then -- Sharpen Impact
		sharpenImpactModule.sharpenImpact()
	elseif Pattern.SelectedPatternIdx == 6 then -- Soften Impact
		softenImpactModule.softenImpact(Pattern.SpeedLimit,SoftenImpact.PercentDistance)
	end
end