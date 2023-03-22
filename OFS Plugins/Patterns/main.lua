local vibrateModule = require("vibrate")
local changingMaxModule = require("changingMax")
local doublerModule = require("doubler")
local fillerModule = require("filler")
local impactBounceModule = require("impactBounce")
local softenImpactModule = require("softenImpact")

local Pattern = {}
Pattern.SelectedPatternIdx = 1

local Vibrate = {}
Vibrate.Speed = 365
Vibrate.TimeBetweenVibrations = 0.05

local ChangingMax = {}
ChangingMax.StartMax = 100
ChangingMax.EndMax = 0

local Filler = {}
Filler.Distance = 40
Filler.Gap = 1

function binding.vibrate()
	vibrateModule.vibrate(Vibrate.Speed,Vibrate.TimeBetweenVibrations)
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

	Pattern.SelectedPatternIdx, changed = ofs.Combo( "", Pattern.SelectedPatternIdx, { "Vibrate", "Changing Max", "Doubler", "Filler", "Impact Bounce", "Soften Impact" } )

	if Pattern.SelectedPatternIdx == 2 then -- Changing Max
		ChangingMax.StartMax, startChanged = ofs.SliderInt("Start Max", ChangingMax.StartMax, 0, 100)
		
		ChangingMax.EndMax, endChanged = ofs.SliderInt("End Max", ChangingMax.EndMax, 0, 100)
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
	elseif Pattern.SelectedPatternIdx == 5 then -- Impact Bounce
		impactBounceModule.impactBounce()
	elseif Pattern.SelectedPatternIdx == 6 then -- Soften Impact
		softenImpactModule.softenImpact()
	end
end