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
		
		vibrateModule.MSBetweenVibrations, densityChanged = ofs.SliderInt("Density(ms)", vibrateModule.MSBetweenVibrations, 30, 130)	
		
		vibrateModule.Invert, invertChanged = ofs.Checkbox( "Invert", vibrateModule.Invert )
		if invertChanged and ofs.Undo() then
			applyPattern()
		end
	elseif Pattern.SelectedPatternIdx == 2 then -- Changing Max
		changingMaxModule.StartMax, startChanged = ofs.SliderInt("Start Max", changingMaxModule.StartMax, 0, 100)
		
		changingMaxModule.EndMax, endChanged = ofs.SliderInt("End Max", changingMaxModule.EndMax, 0, 100)
		
		if ofs.Button( "Swap" ) then
			local oldStart = changingMaxModule.StartMax
			changingMaxModule.StartMax = changingMaxModule.EndMax
			changingMaxModule.EndMax = oldStart
		end 
	elseif Pattern.SelectedPatternIdx == 6 then -- Soften Impact
		softenImpactModule.SoftenAfterTop, changed1 = ofs.Checkbox( "Soften After Top", softenImpactModule.SoftenAfterTop )
		softenImpactModule.AfterTopPercentDistance, changed2 = ofs.SliderInt("AT", softenImpactModule.AfterTopPercentDistance, 10, 60)
		
		softenImpactModule.SoftenBeforeBottom, changed3 = ofs.Checkbox( "Soften Before Bottom", softenImpactModule.SoftenBeforeBottom )
		softenImpactModule.BeforeBottomPercentDistance, changed4 = ofs.SliderInt("BB", softenImpactModule.BeforeBottomPercentDistance, 10, 60)
	
		softenImpactModule.SoftenAfterBottom, changed5 = ofs.Checkbox( "Soften After Bottom", softenImpactModule.SoftenAfterBottom )
		softenImpactModule.AfterBottomPercentDistance, changed6 = ofs.SliderInt("AB", softenImpactModule.AfterBottomPercentDistance, 10, 60)
	
		softenImpactModule.SoftenBeforeTop, changed7 = ofs.Checkbox( "Soften Before Top", softenImpactModule.SoftenBeforeTop )
		softenImpactModule.BeforeTopPercentDistance, changed8 = ofs.SliderInt("BT", softenImpactModule.BeforeTopPercentDistance, 10, 60)		
		
		if (changed1 or changed2 or changed3 or changed4 or changed5 or changed6 or changed7 or changed8) and ofs.Undo() then
			applyPattern()
		end
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
		changingMaxModule.changingMax()
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