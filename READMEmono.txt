For mono support (at least on ubuntu linux, and probably mac os),
one needs to install mono and monodevelop.

(linux) to install mono: <br/>
    sudo apt install apt-transport-https dirmngr <br/>
    sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF <br/>
    echo "deb https://download.mono-project.com/repo/ubuntu vs-bionic main" | sudo tee /etc/apt/sources.list.d/mono-official-vs.list <br/>
    sudo apt update <br/>

(linux) to install monodevelop: <br/>
    sudo apt-get install monodevelop <br/>

Then simply run monodevelop and open the .sln file, clean it, build it, and
run it! Easy!
