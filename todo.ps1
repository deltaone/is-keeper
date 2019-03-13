#enable scripting - Set-ExecutionPolicy RemoteSigned
#default scripting - Set-ExecutionPolicy Restricted

#if need append then set - Add-Content
gci ".\" -r *.cs | ?{ $_.fullname -notmatch "\\refs\\?" } | select-string "TODO:"  -context 0, 4 | Set-Content .todo-src

#explorer $f

