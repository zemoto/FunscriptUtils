function bookend()
	local script = ofs.Script(ofs.ActiveIdx())
	local actionCount = #script.actions

	if actionCount < 2 then
		return
	end

	local newActions = {}
	
	local firstAction = script.actions[1]
	if firstAction.at ~= 0 then
		table.insert(newActions, {at=0, pos=0})
		
		local secondAction = script.actions[2]
		local gap = secondAction.at - firstAction.at;
		
		if firstAction.pos ~= 0 then	
			if gap < firstAction.at then
				table.insert(newActions, {at=(firstAction.at-gap), pos=0})
			end
		else
			if firstAction.at > gap*2 then
				table.insert(newActions, {at=(firstAction.at-(gap*2)), pos=0})
				table.insert(newActions, {at=(firstAction.at-gap), pos=secondAction.pos})
			end
		end
	end
	
	local lastAction = script.actions[actionCount]
	local videoDuration = player.Duration()
	if lastAction.at < videoDuration-0.1 then
		table.insert(newActions, {at=math.min(lastAction.at+1,videoDuration), pos=0})
	end
	
	for idx, newAction in ipairs(newActions) do
		script.actions:add(Action.new(newAction.at, newAction.pos))
	end
	
	script:sort()
	script:commit()
end

return { bookend = bookend }