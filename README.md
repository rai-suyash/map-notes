# Map Notes

## Highlights
- Procedurally and dynamically construct a radial tree
- Insert notes into a radial tree
- Customise notes and tree nodes
- Save and load trees locally

## Overview
A desktop app which allows you to construct a procedurally create a radial tree (rather than manually drag nodes around) and customise notes within tree nodes. You can save, browse, and load several trees, albeit locally.

This program was coded in C# and XAML within the .NET (6.0) framework. SQLite was used to store and load tree data locally.

Originally created for my computer science NEA, it attempts to combat drawbacks in other note-taking apps by:
- allowing the tree to expand in all directions (not simply down, or left and right, like in MindMup)
- and not forcing the user to manually position nodes after tree gets too large (like in Miro).

However, the algorithm this program uses does not account for node sizes, so it includes a textbox in the corner to alter nodes sizes, and distances between parent and child nodes.

This repository is essentially an online store for my project, hence the lack of git commits.

### Authors
[@rai-suyash](https://github.com/rai-suyash/)

## Usage
Open the 'a_level_project' solution in Visual Studio and run it.
