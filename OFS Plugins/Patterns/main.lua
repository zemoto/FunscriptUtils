local vibrateModule = require("vibrate")
local changingMaxModule = require("changingMax")
local doublerModule = require("doubler")
local fillerModule = require("filler")
local sharpenImpactModule = require("sharpenImpact")
local softenImpactModule = require("softenImpact")
local taperModule = require("taper")
local splitModule = require("split")

local Pattern = {}
Pattern.SelectedPatternIdx = 1
Pattern.SpeedLimit = 453

local ChangingMax = {}
ChangingMax.StartMax = 50
ChangingMax.EndMax = 50

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
	
	ofs.Separator()

	Pattern.SelectedPatternIdx, changed = ofs.Combo( "", Pattern.SelectedPatternIdx, { "Vibrate", "Changing Max", "Doubler", "Filler", "Sharpen Impact", "Soften Impact", "Taper", "Split" } )

	if Pattern.SelectedPatternIdx == 1 then -- Vibrate
		vibrateModule.Intensity, intensityChanged = ofs.SliderInt("Intensity(%)", vibrateModule.Intensity, 10, 100)
		if intensityChanged then
			vibrateModule.Speed = Pattern.SpeedLimit * (vibrateModule.Intensity / 100.0)
			if ofs.Undo() then
				applyPattern()
			end
		end
		
		vibrateModule.MSBetweenVibrations, densityChanged = ofs.SliderInt("Density(ms)", vibrateModule.MSBetweenVibrations, 20, 130)	
		
		vibrateModule.Invert, invertChanged = ofs.Checkbox( "Invert", vibrateModule.Invert )
		if invertChanged and ofs.Undo() then
			applyPattern()
		end
	elseif Pattern.SelectedPatternIdx == 2 then -- Changing Max
		ChangingMax.StartMax, startChanged = ofs.SliderInt("Start Max", ChangingMax.StartMax, 0, 100)
		
		ChangingMax.EndMax, endChanged = ofs.SliderInt("End Max", ChangingMax.EndMax, 0, 100)
	elseif Pattern.SelectedPatternIdx == 6 then -- Soften Impact
		softenImpactModule.PercentDistance, changed = ofs.SliderInt("Percent Distance", softenImpactModule.PercentDistance, 10, 60)
		if changed and ofs.Undo() then
			applyPattern()
		end
		
		softenImpactModule.SoftenBeforeTop, changed = ofs.Checkbox( "Soften Before Top", softenImpactModule.SoftenBeforeTop )		
		softenImpactModule.SoftenAfterTop, changed = ofs.Checkbox( "Soften After Top", softenImpactModule.SoftenAfterTop )
		softenImpactModule.SoftenBeforeBottom, changed = ofs.Checkbox( "Soften Before Bottom", softenImpactModule.SoftenBeforeBottom )
		softenImpactModule.SoftenAfterBottom, changed = ofs.Checkbox( "Soften After Bottom", softenImpactModule.SoftenAfterBottom )
		softenImpactModule.SoftenBeforeMiddle, changed = ofs.Checkbox( "Soften Before Middle", softenImpactModule.SoftenBeforeMiddle )
		softenImpactModule.SoftenAfterMiddle, changed = ofs.Checkbox( "Soften After Middle", softenImpactModule.SoftenAfterMiddle )
	elseif Pattern.SelectedPatternIdx == 7 then -- Taper
		if ofs.Button( "Shrink" ) then
			taperModule.shrink()
		end
		
		if ofs.Button( "Grow then Shrink" ) then
			taperModule.growThenShrink()
		end
	end
end

function applyPattern()
	local script = ofs.Script(ofs.ActiveIdx())
	
	if Pattern.SelectedPatternIdx == 1 then -- Vibrate
		vibrateModule.vibrate()
	elseif Pattern.SelectedPatternIdx == 2 then -- Changing Max
		changingMaxModule.changingMax(ChangingMax.StartMax,ChangingMax.EndMax)
	elseif Pattern.SelectedPatternIdx == 3 then -- Doubler
		doublerModule.doubler()
	elseif Pattern.SelectedPatternIdx == 4 then -- Filler
		fillerModule.filler()
	elseif Pattern.SelectedPatternIdx == 5 then -- Sharpen Impact
		sharpenImpactModule.sharpenImpact()
	elseif Pattern.SelectedPatternIdx == 6 then -- Soften Impact
		softenImpactModule.softenImpact(Pattern.SpeedLimit)
	elseif Pattern.SelectedPatternIdx == 7 then -- Taper
	elseif Pattern.SelectedPatternIdx == 8 then -- Split
		splitModule.split()
	end
end