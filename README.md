# Road to Glory (Greedy Mesh Generation) BLOG:
## Dates:
- [20/11/2024](#20112024)
- [21/11/2024](#21112024)
- [22/11/2024](#22112024)
- [23/11/2024](#23112024)
- [24/11/2024](#24112024)
- [25/11/2024](#25112024)
- [27/11/2024](#27112024)

## 20/11/2024:
- after our first meeting when you asked from me to read Building a High-Performance Voxel Engine in Unity artical and try to implement it.
- at first not all was making sense to me so at first i tried copy paste and see what happen, most of the time i got too many errors because some things does not match in Godot as in Unity. so i had to watch many tutorials to make the basics work.
- at first day i did not really progresses although is spent so much time learning things probably most of the day. (5 hours) I think that I spent more but it is okey.
  
the following two days were insane.

## 21/11/2024:
- this day I have started progressing. Of course like every coder when things stops to make sense we delete and start over, so i did that.
- i understood the mesh algorithm way more and started with the basics, with greedy mesh our goal is to render the visable faces for air and what the player sees. So i understand that each face is a square that construct from 2 triangles and each triagle is consist of 3 vertices. of course i know that but this is the key for greedy meshing
- so i succeeded in building a voxel with all of it's faces. and tried to continue with applying it for chunks which is the key here. but i did not progress as much as i thought because I encountered the same error multiple times (regarding PackedVecor3Array) which i sent you an email about. so decied it is enough for today.
- i worked on it for more than 8 hours, this include watching lectures, tutorials and having fun in the editor.

## 22/11/2024:
- I spent all day on the project. i solved the recent error and continued progressing, my goal was to apply greedy meshing for all chunks in the world, i succeeded only for not rendering invisible faces in the chunk it self but not the boundry faces (the ones between each chunk)
- i tried many thing but i did not progress mush here is how it looked [Watch the video](https://drive.google.com/file/d/1U0LGco8grP730v5HRNAZUZpkt0MRTjNm/view?usp=sharing)
- 8 hours.

## 23/11/2024: 
  - Did not work on the project.

## 24/11/2024:
  - Finally i manged to remove the redundant faces from inside the World, i was all the time looking for the wrong thing, the algorithm that they described is correct but it wasn't working for me. i tried to change and tried another things, but the problem was actually how Godot engine handle the scenes ... it not like Unity, so I had to do this to make it work: when generating the world in world.cs i must only create *all the CHUNKS* and  then generate mesh for each chunk on, because when the algortihm was trying to figure out if this face of the current chunk need to visible or not, the other chunk that is trying locate is not build yet. So, that was the probelm it is fixed now.
  - you can [Watch the video](https://drive.google.com/file/d/13EVmF7_wwM1A_7VxCsVTVv4LWW49fPCF/view?usp=sharing)
  - 4 hours.

## 25/11/2024:
  - To make our world look like terrain, we want to use Perlin Noise, but at first we want to do something easier and understand things. We generated noise using Sin function given frequency and amplitude.
  - To choose what is the voxel Type is if Air or Stone I did this:
	- surfaceY = &lambda;
	- $xOffset =  Sin(x * frequency) * amplitude$
	- $zOffset =  Sin(z * frequency) * amplitude$
	- return Stone if $current_voxel.positonY < surfaceY + xOffset + zOffset$ else Air
   
  - I learned this from a video on Youtube, here is the [link](https://www.youtube.com/watch?v=CSa5O6knuwI) if you are interested.

	Here are some photos of the results:
	- classifications are Stone and Air:
	- ![Result 1](results/stone_result.png)
	- To see how greedy meshing works:
	- ![Result 2](results/side_grass_result.png)

   
  - Press [here](https://drive.google.com/file/d/1SbUYPf3C9hNm6X1Xu3zTPQ145w-ETbE8/view?usp=sharing) to see video that visualise my world.
  - 3 hours
## 27/11/2024:

-![Procedural Generation](results/procedural-_generation.png)
- To do procedural generation, in the world class I defined a method that generate more chunks of our world according to the x and z axis in addition to a radius. I knew which chunks must be generated according to the player global position. to be more efficient we made sure not to generate chunks for positions that the player has been in more than once.
- To save resources I also destroyed chunks that are a far from the player global position by using unloalRradius > radius.
- inorder to have more random and smooth terrain, I chnaged the way we get noise. We used GlobalNoise.cs that is given in the arcticle and then we got the noise based on the x and z points then we normalized the noise to stay between [0,1].
- Before i forget, when the player moves in the world and new chunks are generated, if we look inside the world there still boundry mesh between the newly generated terrain and the one that has been geenrated in the previous frame, maybe we can solve it by setting a frame time which will help, but if we did not, the game becomes very slow due to many operations happen at once.
- I did not use texture, only colors, it was easier for to me now.
- 6 hours.
- you can watch the video of procedural generation by clicking [here](https://drive.google.com/file/d/1esg79KLc_E_xlj4RUUYRC174s3Jy_Nlx/view?usp=sharing)

