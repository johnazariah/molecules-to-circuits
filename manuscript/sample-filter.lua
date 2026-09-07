-- Read the complete book, retain selected bodies, and list omitted headings
-- without manufacturing blank chapter pages or fictitious page references.
local manuscript = pandoc.path.directory(PANDOC_SCRIPT_FILE)
local selected = {}
local manifest = assert(io.open(pandoc.path.join({manuscript, "Sample.txt"})))
for filename in manifest:lines() do
    if filename ~= "" and not filename:match("^#") then
        local file = assert(io.open(pandoc.path.join({manuscript, filename})))
        local document = pandoc.read(file:read("*a"), "markdown")
        file:close()
        local found = false
        for _, block in ipairs(document.blocks) do
            if block.t == "Header" and block.level == 1 then
                selected[pandoc.utils.stringify(block)] = true
                found = true
                break
            end
        end
        assert(found, "Sample file has no level-one heading: " .. filename)
    end
end
manifest:close()

local function latex_text(inlines)
    return pandoc.write(pandoc.Pandoc({pandoc.Plain(inlines)}), "latex"):gsub("%s+$", "")
end

function Pandoc(doc)
    local blocks = {
        pandoc.Para({pandoc.Str("Sample edition. Omitted material appears in the contents "
            .. "with a dash instead of a page number; its text is not included.")})
    }
    local included = false
    for _, block in ipairs(doc.blocks) do
        if block.t == "Header" and block.level == 1 then
            included = selected[pandoc.utils.stringify(block)] == true
        end
        if included then
            table.insert(blocks, block)
        elseif block.t == "Header" and block.level <= 2 then
            local level = block.level == 1 and "chapter" or "section"
            table.insert(blocks, pandoc.RawBlock("latex",
                "\\addtocontents{toc}{\\protect\\contentsline{" .. level
                .. "}{\\protect\\textcolor{gray}{" .. latex_text(block.content)
                .. "}}{\\protect\\textemdash}{}}"))
        end
    end
    return pandoc.Pandoc(blocks, doc.meta)
end
