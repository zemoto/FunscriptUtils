-- OFS plugin to take a selected sample of actions, and apply changes made to that sample to other similar groups of actions

local CopySample = {}
CopySample.SampleStart = -1
CopySample.Duration = -1
CopySample.MatchingSamples = {}

function init()
end

function update(delta)
end

function gui()
	if ofs.Button( "Save Sample" ) then
		saveSelectionAsSample()
	end
	
	ofs.SameLine()
	
	if CopySample.SampleStart == -1 or CopySample.Duration == -1 then
		ofs.BeginDisabled(true)
	end
	
	if ofs.Button( "Propagate Changes" ) then
		propagateChanges()
	end
	
	ofs.EndDisabled()
	
	if CopySample.SampleStart ~= -1 and CopySample.Duration ~= -1 then
		ofs.Text( "Sample Saved: " .. formatTime(CopySample.SampleStart) .. " to " .. formatTime(CopySample.SampleStart + CopySample.Duration) )
		ofs.Text( "Matching Samples Found: " .. #CopySample.MatchingSamples )
	end
end

function saveSelectionAsSample()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	CopySample.SampleStart = -1
	CopySample.Duration = -1
	CopySample.MatchingSamples = {}
	
	if not script:hasSelection() then
		return
	end
	
	local timeThreshold = 0.01
	local sampleStart = -1
	local sampleEnd = -1
	local selectionFound = false
	for i=1,actionCount do
		local action = script.actions[i]	
		if action.selected then
			if sampleStart == -1 then
				sampleStart = action.at
			end
			sampleEnd = action.at
			selectionFound = true
		elseif selectionFound then
			break
		end
	end
	
	if sampleStart ~= sampleEnd then
		CopySample.SampleStart = sampleStart
		CopySample.Duration = sampleEnd - sampleStart + timeThreshold
	end
	
	if CopySample.SampleStart == -1 or CopySample.Duration == -1 then
		return
	end
	
	local templateSample = getTemplateSample()
	local templateSampleSize = #templateSample
	local firstSample = templateSample[1]
	local runningSampleStart = -1
	local spotInSample = 1
	local i = 1
	while i <= actionCount do
		local action = script.actions[i]
		local currentSampleAction = templateSample[spotInSample]
		local currentSampleGap = currentSampleAction.at - CopySample.SampleStart
		
		if action.pos == currentSampleAction.pos then		
			if spotInSample ~= 1 then
				local gap = action.at - runningSampleStart
				if currentSampleGap + timeThreshold > gap and gap > currentSampleGap - timeThreshold then
					spotInSample = spotInSample + 1
				else
					runningSampleStart = -1
					spotInSample = 1				
				end
			end
		else
			runningSampleStart = -1
			spotInSample = 1
		end
		
		if spotInSample == 1 and action.pos == firstSample.pos and action.at ~= CopySample.SampleStart then
			runningSampleStart = action.at
			spotInSample = 2	
		end
		
		if templateSampleSize < spotInSample then
			table.insert(CopySample.MatchingSamples, runningSampleStart)
			runningSampleStart = -1
			spotInSample = 1
		else
			i = i + 1
		end
	end
end

function propagateChanges()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	-- Delete contents of current sampleStart
	local j = 1
	for i, sampleStartTime in ipairs(CopySample.MatchingSamples) do
		local sampleEndTime = sampleStartTime + CopySample.Duration
		while j <= actionCount do
			local action = script.actions[j]
			if action.at > sampleEndTime then
				break
			end
			
			if action.at >= sampleStartTime then
				script:markForRemoval(j)
			end
			
			j = j + 1
		end
	end
	
	local templateSample = getTemplateSample()
	local lastAdd = -1
	for i, startTime in ipairs(CopySample.MatchingSamples) do
		for j, templateAction in ipairs(templateSample) do
			local newAt = startTime + (templateAction.at - CopySample.SampleStart)
			if lastAdd + 0.005 < newAt or newAt < lastAdd - 0.005 then
				script.actions:add( Action.new(newAt, templateAction.pos) )
				lastAdd = newAt
			end
		end
	end
	
	script:removeMarked()
	script:commit()
end

function getTemplateSample()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions
	
	local sample = {}
	local inSample = false
	for i=1,actionCount do
		local action = script.actions[i]
		if not inSample then
			if action.at >= CopySample.SampleStart then
				inSample = true
			end
		end		
		if inSample then
			if action.at > CopySample.SampleStart + CopySample.Duration then
				return sample
			end

			table.insert(sample, {at=action.at, pos=action.pos})
		end
	end

	return sample
end

function formatTime(total_seconds)
    local time_hours = math.floor(math.fmod(total_seconds, 86400) / 3600)
    local time_minutes = math.floor(math.fmod(total_seconds, 3600) / 60)
    local time_seconds = math.fmod(total_seconds, 60)
	
	if total_seconds <= 0 then
		return "00:00:00"
	end
	
	local finalString
	if time_seconds < 10 then
		finalString = "0" .. string.format("%.3f", time_seconds)
	else
		finalString = string.format("%.3f", time_seconds)
	end
	
	if time_minutes >= 10 then
		finalString = time_minutes .. ":" .. finalString		
	elseif time_minutes >= 1 then
		finalString = "0" .. time_minutes .. ":" .. finalString
	elseif time_hours >= 1 then
		finalString = "00:" .. finalString
	end
	
	if time_hours >= 10 then
		finalString = time_hours .. ":" .. finalString		
	elseif time_hours >= 1 then
		finalString = "0" .. time_hours .. ":" .. finalString
	end
	
	return finalString
end
