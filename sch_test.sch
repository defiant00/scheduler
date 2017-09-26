start:9/19/2017 8:00 AM

/tasks
;1 50% 30m Do a "thing" [priority:low]
;2 3.5h Do something else
with multiple lines [user:KC]
;g1 a group
> ;s1 sub [start:9/21/2017 8 AM]
> > ;a 1h parallel
> > ;b 2h sub-tasks
with
more descriptions and "a quote" and such
[user:Aaron][priority:Critical]
> > ;c 0.5h after another
> ;4 3h In parallel
;5 2h And after
;6 1.5h Thing!
;7 2d Something awful

/links
1 2 g1 5 7
g1 6 7
b c