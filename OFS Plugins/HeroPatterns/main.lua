local matchPatternModule = require("matchPattern")
local hardModeModule = require("hardMode")
local bookendModule = require("bookend")
local fixTimingModule = require("fixTiming")
local centerModule = require("center")

local HeroPatterns = {}
HeroPatterns.SelectedPatternIdx = 1

local MatchPattern = {}
MatchPattern.IsHardMode = false

local FixTiming = {}
FixTiming.BPM = 128

local Center = {}
Center.Max = 100
Center.Min = 0

function init()
end

function update(delta)
end

function gui()
	if ofs.Button( "Apply Pattern" ) then
		applyPattern()
	end
	
	ofs.Separator()

	HeroPatterns.SelectedPatternIdx, changed = ofs.Combo( "", HeroPatterns.SelectedPatternIdx, { "Match Pattern", "Hard Mode", "Bookend", "Fix Timing", "Center" } )
	
	if HeroPatterns.SelectedPatternIdx == 1 then -- Match Pattern
		MatchPattern.IsHardMode, isHardModeChanged = ofs.Checkbox( "Script is hard mode", MatchPattern.IsHardMode )
	elseif HeroPatterns.SelectedPatternIdx == 4 then -- Fix Timing
		FixTiming.BPM, BPMChanged = ofs.DragInt("BPM", FixTiming.BPM, 1)
	elseif HeroPatterns.SelectedPatternIdx == 5 then -- Center
		Center.Max, maxChanged = ofs.SliderInt("Max", Center.Max, 10, 100)
		Center.Min, minChanged = ofs.SliderInt("Min", Center.Min, 0, 90)
	end
end

function applyPattern()
	if HeroPatterns.SelectedPatternIdx == 1 then -- Match Pattern
		if MatchPattern.IsHardMode then
			matchPatternModule.matchPattern.hard()
		else
			matchPatternModule.matchPattern.easy()
		end
	elseif HeroPatterns.SelectedPatternIdx == 2 then -- Hard Mode
		hardModeModule.hardMode()
	elseif HeroPatterns.SelectedPatternIdx == 3 then -- Bookend
		bookendModule.bookend()
	elseif HeroPatterns.SelectedPatternIdx == 4 then -- Fix Timing
		fixTimingModule.fixTiming(FixTiming.BPM)
	elseif HeroPatterns.SelectedPatternIdx == 5 then -- Center
		centerModule.center(Center.Min,Center.Max)
	end
end