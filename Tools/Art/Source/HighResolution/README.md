# High-resolution authored building masters

These transparent masters preserve the accepted House and Forager Camp designs
at their original generated detail level. They stay outside `Assets/` so Unity
does not import the large source sheets into the game.

Source direction: five medieval 2.5D timber-and-stone House variants with roof
color/material variation, and one open timber/canvas Forager Camp with a work
table, baskets, gathered herbs, and a hanging lantern. Both were authored on a
flat magenta key and normalized to transparent RGBA before entering the project.

Use `Tools/Art/Build-HighResolutionAuthoredBuildings.ps1` to normalize them into
the 2x runtime canvases while retaining the legacy world alignment.

`Construction1x/` retains the accepted pre-upgrade construction atlases used by
`Tools/Art/Upgrade-ConstructionAtlas2x.ps1`; they are tool inputs, not runtime
assets.
