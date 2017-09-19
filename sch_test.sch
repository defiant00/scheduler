/start 9/19/2017 8:00 AM

;1 (50)  Do a thing
;2 (0) 3.5h Do something else
with multiple lines
;g1 a group
> ;s1 sub
> > ;a (0) 1h parallel
> > ;b (0) 2h sub-tasks
> > ;c (0) 0.5h after another
> ;4 (0) 3h In parallel
;5 (0) 2h And after
;6 (0) 1.5h Thing!
;7 (0) 2d Something awful

[1 2 g1 5 7]
[g1 6 7]
[b c]