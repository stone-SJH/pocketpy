#include "pocketpy/error.h"

namespace pkpy{

    SourceData::SourceData(std::string_view source, const Str& filename, CompileMode mode): filename(filename), mode(mode) {
        int index = 0;
        // Skip utf8 BOM if there is any.
        if (strncmp(source.data(), "\xEF\xBB\xBF", 3) == 0) index += 3;
        // Drop all '\r'
        SStream ss(source.size() + 1);
        while(index < source.size()){
            if(source[index] != '\r') ss << source[index];
            index++;
        }
        this->source = ss.str();
        line_starts.push_back(this->source.c_str());
    }

    SourceData::SourceData(const Str& filename, CompileMode mode): filename(filename), mode(mode) {
        line_starts.push_back(this->source.c_str());
    }

    std::pair<const char*,const char*> SourceData::_get_line(int lineno) const {
        if(lineno == -1) return {nullptr, nullptr};
        lineno -= 1;
        if(lineno < 0) lineno = 0;
        const char* _start = line_starts.at(lineno);
        const char* i = _start;
        // max 300 chars
        while(*i != '\n' && *i != '\0' && i-_start < 300) i++;
        return {_start, i};
    }

    Str SourceData::snapshot(int lineno, const char* cursor, std::string_view name) const{
        SStream ss;
        std::string meta = "";
        //write filename as string
        meta += 's';
        meta += filename.escape(false).str();
        meta += '\n';

        //write lineno as int
        meta += 'i';
        meta += std::to_string(lineno);
        meta += '\n';

        //write name as string
        meta += 's';
        if (!name.empty()) meta += name;
        meta += '\n';

        //write column as int
        meta += 'i';

        ss << "  " << "File \"" << filename << "\", line " << lineno;
        if(!name.empty()) ss << ", in " << name;
        if(!source.empty()){
            ss << '\n';
            std::pair<const char*,const char*> pair = _get_line(lineno);
            Str line = "<?>";
            int removed_spaces = 0;
            if(pair.first && pair.second){
                line = Str(pair.first, pair.second-pair.first).lstrip();
                removed_spaces = pair.second - pair.first - line.length();
                if(line.empty()) line = "<?>";
            }
            ss << "    " << line;
            if(cursor && line != "<?>" && cursor >= pair.first && cursor <= pair.second){
                auto column = cursor - pair.first - removed_spaces;
                meta += std::to_string(column);
                if(column >= 0) ss << "\n    " << std::string(column, ' ') << "^";
            }
        }
        meta += '\n';

        return Str(meta) + ss.str();
    } 

    Str Exception::summary() const {
        stack<ExceptionLine> st(stacktrace);
        SStream ss;
        if(is_re) ss << "Traceback (most recent call last):\n";
        while(!st.empty()) {
            ss << st.top().snapshot() << '\n';
            st.pop();
        }
        // TODO: allow users to override the behavior
        if (!msg.empty()) ss << type.sv() << ": " << msg;
        else ss << type.sv();
        return ss.str();
    }

}   // namespace pkpy