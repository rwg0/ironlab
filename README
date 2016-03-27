
IronLab

Copyright (c) 2010 Joe Moorhouse

Available under the GNU Lesser General Public License
http://www.gnu.org/licenses/lgpl.html

Disclaimer:
THIS SOFTWARE IS PROVIDED "AS IS" AND ANY EXPRESSED OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

Building:
You should be able to simply open IronLab.sln and build the solution: all dependencies are included as dlls. This will build into IronLab\bin by default. You will need to put the contents of the IronPython Lib folder into IronLab\bin\Lib yourself if you want the various modules to be present. Without this, for example, typing "help", "copyright", "credits" or "license" will not do much, no matter what the start-up text might say!

Prerequisites:
IronLab uses .NET 4.0. It may be necessary to update DirectX by downloading and running the DirectX Redistributable. This is because IronLab uses the managed wrapper to DirectX, SlimDX, which may demand an updated DirectX. 

Getting started with NumPy/SciPy:
Copy your IronPython 2.7 installation folders (including NumPy and SciPy)
DLLs, Doc, Lib into the build bin directory.
copy ILabPythonLib\ironplot folder into the newly copied bin\Lib\site-packages
Try:
import numpy as np
import ironplot as ip
ip.plot(np.sin(np.arange(0, 10, 0.1)))
then Examples\NumpyPlot content

ILNumerics support is now deprecated.
